
'use strict';
import { randomInt } from "crypto";
import { createSocket, RemoteInfo, Socket } from "dgram";

enum RainSocketHead {
    hookup = 1,
    convention,
    heartbeat,
    message,
}

class RainValue {
    constructor(public type: number, public value: any) { }
}
export class RainBufferGenerator {
    values: RainValue[] = [];
    size: number = 0;
    public pushBool(value: boolean) {
        this.values.push(new RainValue(1, value));
        this.size++;
    }
    public pushInt8(value: number) {
        this.values.push(new RainValue(2, value));
        this.size++;
    }
    public pushInt32(value: number) {
        this.values.push(new RainValue(3, value));
        this.size += 4;
    }
    public pushInt64(value: number) {
        this.values.push(new RainValue(4, value));
        this.size += 8;
    }
    public pushString(value: string) {
        var buf = new TextEncoder().encode(value);
        this.values.push(new RainValue(5, buf));
        this.size += 4 + buf.length;
    }
    public pushBuffer(value: Buffer) {
        this.values.push(new RainValue(6, value));
        this.size += 4 + value.length;
    }
    public generator(): Buffer {
        var result = Buffer.alloc(this.size);
        var offset = 0;
        this.values.forEach(element => {
            switch (element.type) {
                case 1:
                    result.writeInt8(element.value ? 1 : 0, offset++);
                    break;
                case 2:
                    result.writeUint8(element.value, offset++);
                    break;
                case 3:
                    result.writeInt32LE(element.value, offset); offset += 4;
                    break;
                case 4:
                    result.writeBigInt64LE(element.value, offset); offset += 8;
                    break;
                case 5:
                    var arr = element.value as Uint8Array;
                    result.writeInt32LE(arr.length, offset); offset += 4;
                    arr.forEach(v => result.writeUint8(v, offset++));
                    break;
                case 6:
                    var buf = element.value as Buffer;
                    result.writeInt32LE(buf.length, offset); offset += 4;
                    buf.forEach(v => result.writeUint8(v, offset++));
                    break;
            }
        });
        return result;
    }
}
export class RainBufferReader {
    offset: number = 0;
    constructor(private buffer: Buffer) { }
    public readBool(): boolean {
        return this.buffer.readUint8(this.offset++) > 0;
    }
    public readInt8(): number {
        return this.buffer.readUint8(this.offset++);
    }
    public readInt32(): number {
        var result = this.buffer.readInt32LE(this.offset);
        this.offset += 4;
        return result;
    }
    public readInt64(): number {
        var result = Number.parseInt(this.buffer.readBigInt64LE(this.offset).toString());
        this.offset += 8;
        return result;
    }
    public readString(): string {
        return new TextDecoder().decode(this.readBuffer());
    }
    public readBuffer(): Buffer {
        var length = this.readInt32();
        var buf = this.buffer.subarray(this.offset, this.offset + length);
        this.offset += length;
        return buf;
    }
    public getResidueBuffer(): Buffer {
        return this.buffer.subarray(this.offset);
    }
}
export class RainSocket {
    socket?: Socket;
    remote?: RemoteInfo;
    token: number = 0;
    selfID: number = 0;
    port: number = 0;
    listener?: (msg: Buffer) => void;
    timestemp: number = new Date().valueOf();
    public active(): boolean {
        return new Date().valueOf() - this.timestemp < 3000;
    }
    private update() {
        this.timestemp = new Date().valueOf();
    }
    public connet(libraryName: string, ip: RemoteInfo, listener: (msg: Buffer) => void) {
        if (this.socket) this.socket.close();
        this.update();
        this.selfID = randomInt(0x7fff_ffff);
        this.listener = listener;
        this.remote = ip;
        this.socket = createSocket("udp6");
        while (true) {
            try {
                this.port = 8192 + randomInt(8192);
                this.socket.bind(this.port);
                break;
            } catch { }
        }
        this.socket.addListener("message", (msg) => this.onListener(new RainBufferReader(msg)));
        var buf = new RainBufferGenerator();
        buf.pushInt8(RainSocketHead.hookup);
        buf.pushString(libraryName);
        buf.pushInt32(this.selfID);
        buf.pushInt32(this.port);
        this.socket.send(buf.generator(), this.remote.port, this.remote.address);
    }
    private onListener(msg: RainBufferReader) {
        if (!this.socket || !this.remote) return;
        var type = msg.readInt8();
        switch (type) {
            case RainSocketHead.convention: {
                if (this.selfID == msg.readInt32()) {
                    this.update();
                    this.token = msg.readInt32();
                    this.remote.port = msg.readInt32();
                }
            } break;
            case RainSocketHead.heartbeat: {
                if (this.token == msg.readInt32()) {
                    this.update();
                    var buf = new RainBufferGenerator();
                    buf.pushInt8(RainSocketHead.heartbeat);
                    buf.pushInt32(this.selfID);
                    this.socket.send(buf.generator(), this.remote.port, this.remote.address);
                }
            } break;
            case RainSocketHead.message: {
                if (this.token == msg.readInt32()) {
                    this.update();
                    this.listener?.(msg.getResidueBuffer());
                }
            } break;
        }
    }
    public send(msg: Buffer) {
        if (this.socket && this.remote) {
            var buf = new RainBufferGenerator();
            buf.pushInt8(RainSocketHead.message);
            buf.pushInt32(this.selfID);
            buf.pushBuffer(msg);
            this.socket.send(buf.generator(), this.remote.port, this.remote.address);
        }
    }
    public close() {
        this.socket?.close();
        this.socket = undefined;
    }
}