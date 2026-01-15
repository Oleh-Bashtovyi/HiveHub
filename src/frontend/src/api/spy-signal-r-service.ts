import * as signalR from "@microsoft/signalr";
import { type SpyHubEvent, SpyHubMethods, SpyHubEvents } from "../const/spy-game-events";
import type {
    ApiResponse,
    SpyRoomGameSettingsDto,
    CreateRoomResponseDto,
    JoinRoomResponseDto,
    SpyGameEventMap,
    SpyRoomStateDto
} from "../models/spy-game";

type SpyEventCallback<T extends SpyHubEvent> = (data: SpyGameEventMap[T]) => void;

export class SpySignalRService {
    private connection: signalR.HubConnection | null = null;
    private callbacks: Map<string, Set<(data: unknown) => void>> = new Map();

    private hubUrl: string;

    constructor(hubUrl: string) {
        this.hubUrl = hubUrl;
    }

    public async start(): Promise<void> {
        if (this.connection?.state === signalR.HubConnectionState.Connected) return;

        if (!this.connection) {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(this.hubUrl, {
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000])
                .build();

            this.registerInternalListeners();
        }

        if (this.connection.state === signalR.HubConnectionState.Disconnected) {
            try {
                await this.connection.start();
                console.log("SignalR Connected.");
            } catch (err: unknown) {
                console.error("SignalR Connection Error: ", err);
                throw err;
            }
        }
    }

    public stop() {
        this.connection?.stop();
        this.callbacks.clear();
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

    public onTransportReconnected(callback: (connectionId: string | undefined) => void) {
        if (this.connection) {
            this.connection.onreconnected(callback);
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
        return this.invoke<SpyRoomStateDto>(SpyHubMethods.Reconnect, roomCode, oldPlayerId);
    }

    public async leaveRoom(roomCode: string) {
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

    public async updateSettings(roomCode: string, settings: SpyRoomGameSettingsDto) {
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

    public async startAccusation(roomCode: string, targetPlayerId: string) {
        return this.invoke<void>(SpyHubMethods.StartAccusation, roomCode, targetPlayerId);
    }

    public async vote(roomCode: string, targetPlayerId: string, voteType: string | null) {
        const voteTypeString = voteType?.toString();

        console.log("Sending Vote:", { roomCode, targetPlayerId, voteTypeString });

        return this.invoke<void>(SpyHubMethods.Vote, roomCode, targetPlayerId, voteTypeString);
    }

    public async makeGuess(roomCode: string, word: string) {
        return this.invoke<void>(SpyHubMethods.MakeGuess, roomCode, word);
    }

    private registerInternalListeners() {
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