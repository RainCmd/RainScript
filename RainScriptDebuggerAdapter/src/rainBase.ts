
'use strict';

import { createSocket, RemoteInfo, Socket  } from "dgram";
import * as vscode from 'vscode';

var remoteRecver : Socket;
var remoteDebuggers : RemoteDebugSummary[] = []


export class RemoteDebugSummary{
    constructor(public name:string,public ip:RemoteInfo,public timestamp:number){}
    public toString():string{
        return `${this.name} [${this.ip.address}:${this.ip.port}]`;
    }
}
export function sleep(ms:number) {
	return new Promise(v=>setTimeout(v, ms));
} 
export async function selectionRemoteDebugger():Promise<RemoteDebugSummary | undefined> {
    var cur=new Date().valueOf();
    remoteDebuggers=remoteDebuggers.filter(value=>cur-value.timestamp<3000);
	if(remoteDebuggers.length>0){
		var select=await vscode.window.showQuickPick(remoteDebuggers.map((value,index)=>`${index} ${value.name} [${value.ip.address}:${value.ip.port}]`),{
			title:"选择运行中的调试器"
		});
		if(select){
			var match=/^([0-9])/g.exec(select);
			if(match){
				var index = Number.parseInt(match[0]);
				return remoteDebuggers[index];
			}
		}
	}else{
        vscode.window.showInformationMessage("没有监听到远程调试适配器");
    }
	return undefined;
}

export function activate(){
	remoteRecver=createSocket("udp6");
    remoteRecver.bind(38465);
	remoteRecver.addListener("message",(buf,info)=>
	{
		var port=buf.readInt32LE();
		info={
			address:info.address,
			family:info.family,
			port:port,
			size:info.size
		};
		var msg=new TextDecoder().decode(buf.subarray(4));
		var result= remoteDebuggers.find(v=>v.ip.address==info.address&&v.ip.port==info.port&&v.name==msg);
		if(result){
			result.timestamp=new Date().valueOf();
		}
		else{
            result={
                name : msg,
                ip : info,
                timestamp : new Date().valueOf()
            };
			remoteDebuggers.push(result);
		}
	});
}

export function deactivate() {
	remoteRecver.close();
}