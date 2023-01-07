import { EventEmitter, Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Guid } from "guid-typescript";
import * as idb from 'idb/with-async-ittr';
import { DBSchema, IDBPDatabase } from 'idb/with-async-ittr';

@Injectable({
    providedIn: 'root'
})
export class WebsocketService {
    public connectionStatus: ConnectionStatus = ConnectionStatus.Connecting;
    private initalizeTimeout: any | null = null;
    private webSocket: WebSocket | null = null;
    private messageQueue: WebSocketMessageQueueItem[] = [];
    private responseCallbacks: {
        [key: string]: (response: WebSocketResponseMessage | null) => void;
    } = {};
    private wsRequests: {
        [key: string]: string;
    } = {};
    public authenticationResolved!: Promise<void>;
    private authenticationResolve!: ((connectionStatus: void) => void);
    public onConnectionStatusChanged: EventEmitter<ConnectionStatus> = new EventEmitter<ConnectionStatus>();
    public onReconnected: EventEmitter<void> = new EventEmitter<void>();
    private eventSubscribers: {
        [key: string]: EventEmitter<WebSocketMessage<any>>;
    } = {};
    public connectionIsAuthenticated = false;
    public messageBacklogChanged: EventEmitter<number> = new EventEmitter<number>();
    private connectionAttempt = 0;
    private cacheDb: IDBPDatabase<CacheDb> | null = null;
    private wasConnected = false;

    public constructor() {
        this.initDb();
        this.initalize();
    }

    private async initDb(): Promise<void> {
        try {
            this.cacheDb = await idb.openDB<CacheDb>('OverwatchCache', 1, {
                upgrade(db) {
                    db.createObjectStore('ws');
                },
            });
        }
        catch (e) {
            console.error(e);
            this.cacheDb = null;
        }
    }

    public reconnect(): void {
        if (this.initalizeTimeout !== null) {
            clearTimeout(this.initalizeTimeout);
            this.initalizeTimeout = null;
        }
        this.initalize();
    }

    public disconnect(): void {
        if (this.webSocket?.readyState === 1) {
            this.setConnectionStatus(ConnectionStatus.Disconnected);
            this.authenticationResolved = new Promise((resolve) => {
                this.authenticationResolve = resolve;
            });
            this.authenticationResolve();
            this.webSocket?.close();
            this.webSocket = null;
        }
    }

    private setConnectionStatus(connectionStatus: ConnectionStatus): void {
        this.connectionStatus = connectionStatus;
        this.onConnectionStatusChanged.emit(connectionStatus);
        if (this.connectionStatus === ConnectionStatus.Connected) {
            if (this.wasConnected) {
                this.onReconnected.emit();
            }
            else {
                this.wasConnected = true;
            }
        }
    }

    private initalize(): void {
        this.authenticationResolved = new Promise((resolve) => {
            this.authenticationResolve = resolve;
        });
        this.failCallbacks();
        this.setConnectionStatus(ConnectionStatus.Connecting);
        let webSocketUrl = ((window.location.protocol === "http:") ? "ws://" : "wss://") + window.location.hostname;
        if (environment.websocketPort) {
            webSocketUrl += ":" + environment.websocketPort;
        }
        webSocketUrl += "/ws";
        this.webSocket = new WebSocket(webSocketUrl);
        this.webSocket.onopen = () => {
            if (!environment.production) {
                console.log("WebSocket.onopen");
            }
        };
        this.webSocket.onclose = (event: CloseEvent) => {
            if (!environment.production) {
                console.log("WebSocket.onclose", event);
            }
            if (!event.wasClean) {
                if (!environment.production) {
                    console.log("Unclean close. Scheduling another connection in 10s.");
                }
            }
            if (this.initalizeTimeout !== null) {
                clearTimeout(this.initalizeTimeout);
            }
            const timeout = this.connectionAttempt > 5 ? 30000 : 5000;
            this.initalizeTimeout = setTimeout(() => {
                this.initalize();
            }, timeout);
            this.connectionAttempt++;
            this.setConnectionStatus(ConnectionStatus.Disconnected);
            this.failCallbacks();
        };
        this.webSocket.onerror = (event: Event) => {
            if (!environment.production) {
                console.log("WebSocket.onerror", event);
            }
        };
        this.webSocket.onmessage = (event: MessageEvent) => {
            if (!environment.production) {
                console.log("WebSocket.onmessage", event);
            }
            const message: WebSocketResponseMessage = JSON.parse(event.data);
            this.processMessage(message, false);
        };
    }

    private failCallbacks(): void {
        for (const key in this.responseCallbacks) {
            this.responseCallbacks[key](null);
        }
        this.responseCallbacks = {};
        this.triggerMessageBacklogEvent();
    }

    private async processMessage(message: WebSocketResponseMessage, isCached: boolean): Promise<void> {
        if (message.Name === "Authentication") {
            const authenticationData: WebSocketMessageAuthenticationData = message.Data as any;
            this.connectionIsAuthenticated = authenticationData.IsAuthenticated;
            this.setConnectionStatus(ConnectionStatus.Connected);
            this.connectionAttempt = 0;
            for (const queueItem of this.messageQueue) {
                this.sendMessageInternal(queueItem.message, !queueItem.message.CacheId, queueItem.callback);
            }
            this.messageQueue = [];
            if (this.authenticationResolve) {
                this.authenticationResolve();
            }
        }
        else {
            if (!isCached) {
                const cacheId = this.wsRequests[message.MessageId];
                if (cacheId) {
                    delete this.wsRequests[message.MessageId];
                    if (!environment.production) {
                        console.log(message, cacheId);
                    }
                    if (this.cacheDb) {
                        this.cacheDb.put('ws', message, cacheId);
                    }
                }
            }
            if (message.MessageId && this.responseCallbacks[message.MessageId]) {
                const callback = this.responseCallbacks[message.MessageId];
                delete this.responseCallbacks[message.MessageId];
                try {
                    callback(message);
                }
                catch (e) {
                    console.error(e);
                }
            }
            else if (typeof this.eventSubscribers[message.Name] !== 'undefined') {
                this.eventSubscribers[message.Name].emit(message);
            }
            else {
                console.warn("Unprocessed message", message);
            }
        }
        this.triggerMessageBacklogEvent();
    }

    private async hash(input: string): Promise<string> {
        const utf8 = new TextEncoder().encode(input);
        const hashBuffer = await crypto.subtle.digest('SHA-256', utf8);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashHex = hashArray
            .map((bytes) => bytes.toString(16).padStart(2, '0'))
            .join('');
        return hashHex;
    }

    public sendMessage(name: string, data: any, isUnique: boolean = false): void {
        const message: WebSocketRequestMessage = {
            Name: name,
            Data: data,
            MessageId: Guid.create().toString(),
        };
        this.sendMessageInternal(message, isUnique);
    }

    public sendMessageAndWaitForResponse<T>(name: string, data: any): Promise<WebSocketResponseMessage<T> | null> {
        const message: WebSocketRequestMessage = {
            Name: name,
            Data: data,
            MessageId: Guid.create().toString(),
        };
        let messageResolve;
        const result: Promise<WebSocketResponseMessage<T> | null> = new Promise((resolve) => { messageResolve = resolve; });
        this.sendMessageInternal(message, true, messageResolve);
        return result;
    }

    private async sendMessageInternal(message: WebSocketRequestMessage, isUnique: boolean, callback?: (response: WebSocketResponseMessage | null) => void): Promise<void> {
        if (!isUnique && !message.CacheId) {
            message.CacheId = await this.hash(message.Name + JSON.stringify(message.Data));
        }
        if (this.connectionStatus === ConnectionStatus.Connected && this.webSocket !== null) {
            if (callback && message.MessageId) {
                this.responseCallbacks[message.MessageId] = callback;
            }
            if (!environment.production) {
                console.log("sendMessageInternal", message);
            }
            this.webSocket.send(JSON.stringify(message));
        }
        else {
            this.messageQueue.push({
                message: message,
                callback: callback,
            });
        }
        if (message.CacheId) {
            this.wsRequests[message.MessageId] = message.CacheId;
            if (!callback && this.cacheDb) {
                const cachedMessage: WebSocketResponseMessage | undefined = await this.cacheDb.get('ws', message.CacheId);
                if (cachedMessage) {
                    if (!environment.production) {
                        console.log("Using cached message", cachedMessage);
                    }
                    cachedMessage.MessageId = "00000000-0000-0000-0000-000000000000";
                    await this.processMessage(cachedMessage, false);
                }
            }
        }
        this.triggerMessageBacklogEvent();
    }

    private triggerMessageBacklogEvent(): void {
        const backlog = Object.keys(this.wsRequests).length + this.messageQueue.length;
        this.messageBacklogChanged.emit(backlog);
    }

    public on<T>(name: string): EventEmitter<WebSocketMessage<T>> {
        if (typeof this.eventSubscribers[name] === 'undefined') {
            this.eventSubscribers[name] = new EventEmitter<WebSocketMessage<any>>();
        }
        return this.eventSubscribers[name];
    }
}

export enum ConnectionStatus {
    Connecting,
    Connected,
    Disconnected,
}

export interface WebSocketMessage<T = unknown> {
    Name: string;
    Data: T;
    MessageId: string;
}

export interface WebSocketRequestMessage<T = unknown> extends WebSocketMessage<T> {
    CacheId?: string;
}

export interface WebSocketResponseMessage<T = unknown> extends WebSocketMessage<T> {
    Success: boolean;
    Errors?: string[];
}

interface WebSocketMessageQueueItem {
    message: WebSocketRequestMessage;
    callback?: (response: WebSocketResponseMessage | null) => void;
}

interface WebSocketMessageAuthenticationData {
    IsAuthenticated: boolean;
}

interface CacheDb extends DBSchema {
    'ws': { key: string, value: WebSocketResponseMessage };
}