import { EventEmitter, Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Guid } from "guid-typescript";
import * as idb from 'idb/with-async-ittr';
import { DBSchema, IDBPDatabase } from 'idb/with-async-ittr';

@Injectable({
    providedIn: 'root'
})
export class WebsocketService {
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
    public onReady: EventEmitter<boolean> = new EventEmitter<boolean>();
    private eventSubscribers: {
        [key: string]: EventEmitter<WebSocketMessage<any>>;
    } = {};
    public connectionIsAuthenticated = false;
    public connectionIsAuthenticatedChanged: EventEmitter<void> = new EventEmitter<void>();
    public messageBacklogChanged: EventEmitter<number> = new EventEmitter<number>();
    private connectionAttempt = 0;
    private cacheDb: IDBPDatabase<CacheDb> | null = null;
    private wasConnected = false;
    public constructor() {
        this.initDb();
        this.initalize();
        window.addEventListener("online", () => {
            this.initalizeConnection(false);
        });
        window.addEventListener("pageshow", () => {
            if (!environment.production) {
                console.log("pageshow event");
            }
            this.initalizeConnection(false);
        });
    }

    public ensureConnected(): void {
        if (!environment.production) {
            console.log("ensureConnected");
        }
        if (this.connectionStatus === ConnectionStatus.Closed) {
            this.initalizeConnection(false);
        }
    }

    public get connectionStatus(): ConnectionStatus {
        return this.webSocket?.readyState ?? ConnectionStatus.Closed;
    }

    private async initDb(): Promise<void> {
        try {
            this.cacheDb = await idb.openDB<CacheDb>('OverwatchCache', 1, {
                upgrade(db) {
                    db.createObjectStore('ws');
                },
            });
            if (this.cacheDb) {
                for (const queueItem of this.messageQueue) {
                    await this.checkMessageCache(queueItem.message, !!queueItem.callback);
                }
            }
        }
        catch (e) {
            console.error(e);
            this.cacheDb = null;
        }
    }

    public reconnect(): void {
        if (!environment.production) {
            console.log("reconnect");
        }
        if (this.webSocket?.readyState == ConnectionStatus.Open) {
            this.disconnect();
        }
        if (this.initalizeTimeout !== null) {
            clearTimeout(this.initalizeTimeout);
            this.initalizeTimeout = null;
        }
        this.connectionIsAuthenticated = false;
        this.initalize();
    }

    public disconnect(): void {
        if (this.connectionStatus === ConnectionStatus.Open) {
            this.authenticationResolved = new Promise((resolve) => {
                this.authenticationResolve = resolve;
            });
            this.authenticationResolve();
            this.webSocket?.close();
            this.webSocket = null;
        }
    }

    private connectionStatusChanged(): void {
        this.onConnectionStatusChanged.emit(this.connectionStatus);
    }

    private initalizeConnection(delayed: boolean = true): void {
        this.connectionStatusChanged();
        if (delayed) {
            const timeout = this.connectionAttempt > 5 ? 30000 : 5000;
            this.initalizeTimeout = setTimeout(() => {
                this.initalize();
            }, timeout);
        }
        else {
            this.initalize();
        }
    }

    private initalize(): void {
        if (!navigator.onLine) {
            return;
        }
        if (this.webSocket != null &&
            (this.webSocket.readyState == ConnectionStatus.Connecting || this.webSocket.readyState == ConnectionStatus.Open)) {
            return;
        }
        this.authenticationResolved = new Promise((resolve) => {
            this.authenticationResolve = resolve;
        });
        this.failCallbacks();
        this.connectionAttempt++;
        let webSocketUrl = ((window.location.protocol === "http:") ? "ws://" : "wss://") + window.location.hostname;
        if (environment.websocketPort) {
            webSocketUrl += ":" + environment.websocketPort;
        }
        webSocketUrl += "/ws";
        this.webSocket = new WebSocket(webSocketUrl);
        this.connectionStatusChanged();
        this.webSocket.onopen = () => {
            if (!environment.production) {
                console.log("WebSocket.onopen");
            }
            this.connectionStatusChanged();
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
            this.initalizeConnection();
            this.failCallbacks();
        };
        this.webSocket.onerror = (event: Event) => {
            if (!environment.production) {
                console.log("WebSocket.onerror", event);
            }
            this.connectionStatusChanged();
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
            if (this.connectionIsAuthenticated !== authenticationData.IsAuthenticated) {
                this.connectionIsAuthenticated = authenticationData.IsAuthenticated;
                this.connectionIsAuthenticatedChanged.emit();
            }
            if (this.connectionStatus === ConnectionStatus.Open) {
                this.onReady.emit(this.wasConnected);
                this.wasConnected = true;
            }
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
        if (this.webSocket !== null && this.connectionStatus === ConnectionStatus.Open) {
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
        await this.checkMessageCache(message, !!callback);
        this.triggerMessageBacklogEvent();
    }

    private async checkMessageCache(message: WebSocketRequestMessage, hasCallback: boolean): Promise<void> {
        if (message.CacheId) {
            this.wsRequests[message.MessageId] = message.CacheId;
            if (!hasCallback && this.cacheDb) {
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
    Connecting = 0,
    Open,
    Closing,
    Closed,
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