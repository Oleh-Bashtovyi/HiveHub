import * as signalR from "@microsoft/signalr";
import {type SpyHubEvent, SpyHubMethods, SpyHubEvents} from "../const/spy-game-events";
import type {
    ApiResponse,
    RoomGameSettingsDto,
    CreateRoomResponseDto,
    JoinRoomResponseDto,
    SpyGameEventMap, RoomStateDto
} from "../models/spy-game";

type SpyEventCallback<T extends SpyHubEvent> = (data: SpyGameEventMap[T]) => void;

export class SpySignalRService {
    private connection: signalR.HubConnection | null = null;
    private callbacks: Map<string, Set<(data: unknown) => void>> = new Map();
    private currentRoomCode: string | null = null;
    private savedPlayerId: string | null = null;
    private hubUrl: string;

    constructor(hubUrl: string) {
        this.hubUrl = hubUrl;
    }

    public async start(): Promise<void> {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            console.log("SignalR already connected.");
            return;
        }

        if (!this.connection) {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(this.hubUrl, {
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .build();

            this.registerInternalListeners();
        }

        if (this.connection.state === signalR.HubConnectionState.Disconnected) {
            try {
                await this.connection.start();
                console.log("SignalR Connected.");
                await this.tryAutoReconnect();
            } catch (err: unknown) {
                if (err instanceof Error && err.message.includes("before stop() was called")) {
                    console.debug("Connection cancelled by unmount (clean exit).");
                    return;
                }
                console.error("SignalR Connection Error: ", err);
                throw err;
            }
        }
    }

    public stop() {
        this.connection?.stop();
        this.callbacks.clear();
    }

    public setSessionInfo(roomCode: string, playerId: string) {
        this.currentRoomCode = roomCode;
        this.savedPlayerId = playerId;
        sessionStorage.setItem('hive_room', roomCode);
        sessionStorage.setItem('hive_player', playerId);
    }

    public clearSessionInfo() {
        this.currentRoomCode = null;
        this.savedPlayerId = null;
        sessionStorage.removeItem('hive_room');
        sessionStorage.removeItem('hive_player');
    }

    private async tryAutoReconnect() {
        const room = this.currentRoomCode || sessionStorage.getItem('hive_room');
        const player = this.savedPlayerId || sessionStorage.getItem('hive_player');

        if (room && player && this.connection?.state === signalR.HubConnectionState.Connected) {
            console.log(`Attempting reconnect to room ${room}...`);
            try {
                const result = await this.reconnect(room, player);
                console.log("Reconnected successfully!", result);
            } catch (e) {
                console.warn("Reconnect failed:", e);
                this.clearSessionInfo();
            }
        }
    }

    private async invoke<T>(methodName: string, ...args: unknown[]): Promise<T> {
        if (!this.connection) throw new Error("No SignalR connection");

        try {
            const response = await this.connection.invoke<ApiResponse<T>>(methodName, ...args);
            if (!response.success) {
                throw new Error(response.error || "Unknown server error");
            }
            return response.data as T;
        } catch (error) {
            console.error(`Error invoking ${methodName}:`, error);
            throw error;
        }
    }

    public async createRoom() {
        return this.invoke<CreateRoomResponseDto>(SpyHubMethods.CreateRoom);
    }

    public async joinRoom(roomCode: string) {
        return this.invoke<JoinRoomResponseDto>(SpyHubMethods.JoinRoom, roomCode);
    }

    public async reconnect(roomCode: string, oldPlayerId: string) {
        return this.invoke<RoomStateDto>(SpyHubMethods.Reconnect, roomCode, oldPlayerId);
    }

    public async leaveRoom(roomCode: string) {
        this.clearSessionInfo();
        return this.invoke<void>(SpyHubMethods.LeaveRoom, roomCode);
    }

    public async changeName(roomCode: string, newName: string) {
        return this.invoke<void>(SpyHubMethods.ChangeName, roomCode, newName);
    }

    public async changeAvatar(roomCode: string, avatarId: string) {
        return this.invoke<void>(SpyHubMethods.ChangeAvatar, roomCode, avatarId);
    }

    public async toggleReady(roomCode: string) {
        return this.invoke<void>(SpyHubMethods.ToggleReady, roomCode);
    }

    public async changeHost(roomCode: string, newHostPlayerId: string) {
        return this.invoke<void>(SpyHubMethods.ChangeHost, roomCode, newHostPlayerId);
    }

    public async kickPlayer(roomCode: string, targetPlayerId: string) {
        return this.invoke<void>(SpyHubMethods.KickPlayer, roomCode, targetPlayerId);
    }

    public async updateSettings(roomCode: string, settings: RoomGameSettingsDto) {
        return this.invoke<void>(SpyHubMethods.UpdateSettings, roomCode, settings);
    }

    public async returnToLobby(roomCode: string) {
        return this.invoke<void>(SpyHubMethods.ReturnToLobby, roomCode);
    }

    public async startGame(roomCode: string) {
        return this.invoke<void>(SpyHubMethods.StartGame, roomCode);
    }

    public async sendMessage(roomCode: string, message: string) {
        return this.invoke<void>(SpyHubMethods.SendMessage, roomCode, message);
    }

    public async voteStopTimer(roomCode: string) {
        return this.invoke<void>(SpyHubMethods.VoteStopTimer, roomCode);
    }

    public async revealSpies(roomCode: string) {
        return this.invoke<void>(SpyHubMethods.RevealSpies, roomCode);
    }

    public on<K extends SpyHubEvent>(event: K, callback: SpyEventCallback<K>) {
        if (!this.callbacks.has(event)) {
            this.callbacks.set(event, new Set());
        }
        this.callbacks.get(event)?.add(callback as (data: unknown) => void);
    }

    public off<K extends SpyHubEvent>(event: K, callback: SpyEventCallback<K>) {
        this.callbacks.get(event)?.delete(callback as (data: unknown) => void);
    }

    private registerInternalListeners() {
        if (!this.connection) return;

        this.connection.onreconnected(async (connectionId) => {
            console.log(`Connection reestablished. New ID: ${connectionId}`);
            await this.tryAutoReconnect();
        });

        Object.values(SpyHubEvents).forEach((event) => {
            this.connection?.on(event, (data: unknown) => {
                console.log(`[SignalR] ${event}:`, data);
                const handlers = this.callbacks.get(event);
                if (handlers) {
                    handlers.forEach((cb) => cb(data));
                }
            });
        });
    }
}