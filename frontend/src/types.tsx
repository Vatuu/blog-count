export type Post = {
    readonly title: string;
    readonly count: Map<string, number>;
}

export type SocketState = {
    message: string;
    state: SocketStateEnum;
}

export enum SocketStateEnum {
    PENDING = "Pending",
    OPEN = "Ready",
    CLOSED = "Closed",
    ERROR = "Error"
}  

export enum ChangeReason {
    REMOVED = "Removed",
    ADDED = "Added",
    MODIFIED = "Modified"
}

export interface AppProps {
    port: number;
}

export interface ContainerProps {
    socket: WebSocket;
}

export interface SocketStateProps {
    state: SocketState;
}

export interface PostProps {
    id: number;
    date: Date;
    post: Post;
}