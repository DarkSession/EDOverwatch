import { EventEmitter, Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Guid } from "guid-typescript";

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
    public authenticationResolved!: Promise<void>;
    private authenticationResolve!: ((connectionStatus: void) => void);
    public onConnectionStatusChanged: EventEmitter<ConnectionStatus> = new EventEmitter<ConnectionStatus>();
    private eventSubscribers: {
        [key: string]: EventEmitter<WebSocketMessage<any>>;
    } = {};

    public constructor() {
        this.initalize();
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
            this.setConnectionStatus(ConnectionStatus.NotAuthenticated);
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
        webSocketUrl + "/ws";
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
            if (!event.wasClean && this.connectionStatus == ConnectionStatus.Authenticated) {
                if (this.initalizeTimeout !== null) {
                    clearTimeout(this.initalizeTimeout);
                }
                this.initalizeTimeout = setTimeout(() => {
                    this.initalize();
                }, 10000);
                if (!environment.production) {
                    console.log("Unclean close. Scheduling another connection in 10s.");
                }
            }
            if (this.connectionStatus !== ConnectionStatus.NotAuthenticated) {
                this.setConnectionStatus(ConnectionStatus.NoConnection);
                this.failCallbacks();
            }
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
            this.processMessage(message);
        };
    }

    private failCallbacks(): void {
        for (const key in this.responseCallbacks) {
            this.responseCallbacks[key](null);
        }
        this.responseCallbacks = {};
    }

    private async processMessage(message: WebSocketResponseMessage): Promise<void> {
        switch (message.Name) {
            case "Authentication": {
                const authenticationData: WebSocketMessageAuthenticationData = message.Data as any;
                if (authenticationData.IsAuthenticated) {
                    this.setConnectionStatus(ConnectionStatus.Authenticated);
                    for (const queueItem of this.messageQueue) {
                        this.sendMessageInternal(queueItem.message, queueItem.callback);
                    }
                    this.messageQueue = [];
                }
                else {
                    this.setConnectionStatus(ConnectionStatus.NotAuthenticated);
                }
                if (this.authenticationResolve) {
                    this.authenticationResolve();
                }
                break;
            }
            default: {
                if (message.MessageId && this.responseCallbacks[message.MessageId]) {
                    const callback = this.responseCallbacks[message.MessageId];
                    delete this.responseCallbacks[message.MessageId];
                    callback(message);
                }
                else if (typeof this.eventSubscribers[message.Name] !== 'undefined') {
                    this.eventSubscribers[message.Name].emit(message);
                }
                else {
                    console.warn("Unprocessed message", message);
                }
            }
        }
    }

    public sendMessage(name: string, data: any): void {
        const message: WebSocketMessage = {
            Name: name,
            Data: data,
        };
        this.sendMessageInternal(message);
    }

    public sendMessageAndWaitForResponse<T>(name: string, data: any): Promise<WebSocketResponseMessage<T> | null> {
        const message: WebSocketMessage = {
            Name: name,
            Data: data,
            MessageId: Guid.create().toString(),
        };
        let messageResolve;
        const result: Promise<WebSocketResponseMessage<T> | null> = new Promise((resolve) => { messageResolve = resolve; });
        this.sendMessageInternal(message, messageResolve);
        return result;
    }

    private sendMessageInternal(message: WebSocketMessage, callback?: (response: WebSocketResponseMessage | null) => void): void {
        if (this.connectionStatus === ConnectionStatus.Authenticated && this.webSocket !== null) {
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
    NotAuthenticated,
    Authenticated,
    NoConnection,
}

export interface WebSocketMessage<T = unknown> {
    Name: string;
    Data: T;
    MessageId?: string;
}

export interface WebSocketResponseMessage<T = unknown> extends WebSocketMessage<T> {
    Success: boolean;
    Errors?: string[];
}

interface WebSocketMessageQueueItem {
    message: WebSocketMessage;
    callback?: (response: WebSocketResponseMessage | null) => void;
}

interface WebSocketMessageAuthenticationData {
    IsAuthenticated: boolean;
}