/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/
import {
	LoggingDebugSession, InitializedEvent,
	Thread, Scope, StoppedEvent, Variable, TerminatedEvent
} from '@vscode/debugadapter';
import { DebugProtocol } from '@vscode/debugprotocol';
import {RemoteDebugSummary, selectionRemoteDebugger, sleep} from "./rainBase";
import { RainBufferGenerator, RainBufferReader, RainSocket } from './RainNet';
import * as vscode from 'vscode';
import { Subject } from 'await-notify';

/**
 * 此接口描述模拟调试特定的启动属性(不是调试适配器协议的一部分)。
 * 这些属性的模式存在于包中。模拟调试扩展的Json。
 * 接口应该始终匹配此模式。
 */
interface ILaunchRequestArguments extends DebugProtocol.LaunchRequestArguments {
	libraryName : string;
	projectPath : string;
}
interface IAttachRequestArguments extends ILaunchRequestArguments { }

enum SCProto
{
	Reply = 0xffff,
	Continue=0x1001,
	Next,
	Pause,
	SetExceptionFilter,
	ClearBreakpoint,
	GetCoroutines,
	SetVariable,
	GetVariable,
	GetHeap,
	GetStack,
	GetHover,

	HitBreakpoint = 0x2001,
	Exception,
	Terminated,
}


class CoroutineException{
	constructor(public code:Number,public text:string,public stack:string){}
}

export class RainDebugSession extends LoggingDebugSession {
	socket:RainSocket=new RainSocket();
	requestIndex:number=1;
	private requests=new Map<number,RainBufferReader>();
	private exceptions=new Map<number,CoroutineException>();
	remoteSummary?:RemoteDebugSummary;
	private _configurationDone = new Subject();
	libraryName:string="未知的程序集";
	projectPath:string="未定义的工程路径";
	/**
	 * 创建用于一个调试会话的新调试适配器。
	 * 我们在这里配置一个调试适配器的默认实现。
	 */
	public constructor() {
		super();

		// 这个调试器使用从零开始的行和列
		this.setDebuggerLinesStartAt1(false);
		this.setDebuggerColumnsStartAt1(false);
	}

	private OnRecv(buf:Buffer) {
		var rbuf=new RainBufferReader(buf);
		var instruction=rbuf.readInt32();
		switch(instruction){
			case SCProto.Reply:{
				var req=rbuf.readInt32();
				this.requests.set(req,rbuf);
			}break;

			case SCProto.HitBreakpoint:{
				var tid=rbuf.readInt32();
				var reason=rbuf.readString();
				this.sendEvent(new StoppedEvent(reason,tid));
			}break;
			case SCProto.Exception:{
				var id=rbuf.readInt32();
				var code=rbuf.readInt64();
				var text=rbuf.readString();
				var stack = rbuf.readString();
				this.exceptions.set(id,{
					code : code,
					text : text,
					stack : stack
				});
				this.sendEvent(new StoppedEvent('exception',id,text));
			}break;
		}
	}
	private async Request(instruction:SCProto,buf?:RainBufferGenerator):Promise<RainBufferReader|undefined>{
		if(!this.socket)return undefined;
		var sbuf=new RainBufferGenerator();
		var index=this.requestIndex++;
		sbuf.pushInt32(instruction.valueOf());
		sbuf.pushInt32(index);
		if(buf){
			sbuf.pushBuffer(buf.generator());
		}
		var idx=0
		do{
			this.socket.send(sbuf.generator());
			var i=3;
			var to=1000;
			while(!this.requests.has(index)&&to>0)
			{
				to-=i;
				await sleep(i++);
			}
			var result=this.requests.get(index);
			this.requests.delete(index);
		}while(!result&&idx++<3)
		if(to<=0){
			this.sendEvent(new TerminatedEvent());
			vscode.window.showInformationMessage(`请求${instruction}超时，已断开 ${this.remoteSummary?.name} 连接`);
		}
		return result;
	}
	/**
	 * 'initialize'请求是前端调用的第一个请求，用于询问调试适配器提供的特性。
	 */
	protected async initializeRequest(response: DebugProtocol.InitializeResponse, args: DebugProtocol.InitializeRequestArguments): Promise<void> {

		this.requests.clear();
		this.remoteSummary = await selectionRemoteDebugger();
		if(this.remoteSummary){
			if(this.socket)this.socket.close();
			this.socket.connet(this.libraryName,this.remoteSummary.ip,buf => this.OnRecv(buf));
		}else{
			this.sendEvent(new TerminatedEvent());
		}

		//客户端支持进度报告。
		if (args.supportsProgressReporting) {
			
		}
		//客户端支持无效事件。
		if (args.supportsInvalidatedEvent) {
			
		}

		response.body = response.body || {};// 构建并返回此调试适配器的功能:
		response.body.supportsConfigurationDoneRequest = true;// 适配器实现configurationDone请求。
		response.body.supportsEvaluateForHovers = true;// 让VS Code使用'evaluate'当鼠标悬停在源代码上
		response.body.supportsStepBack = false;// 让VS Code显示一个“后退”按钮
		response.body.supportsDataBreakpoints = false;// 使VS Code支持数据断点

		// 使VS Code支持在REPL中完成
		response.body.supportsCompletionsRequest = false;
		response.body.completionTriggerCharacters = [ ".", "[" ];

		response.body.supportsCancelRequest = true;// 使VS Code发送取消请求
		response.body.supportsBreakpointLocationsRequest = true;// 让VS Code发送breakpointLocations请求
		response.body.supportsStepInTargetsRequest = false;// 让VS Code提供“步进目标”功能

		// 条件异常过滤器
		response.body.supportsExceptionFilterOptions = true;
		response.body.exceptionBreakpointFilters = [
			{
				filter: '退出码',
				label: "指定退出码",
				description: `只有当退出码等于输入的退出码时才会引发断点`,
				default: true,
				supportsCondition: true,
				conditionDescription: `输入退出码`
			}
		];

		response.body.supportsExceptionInfoRequest = true;// 让VS Code发送exceptionInfo请求
		response.body.supportsSetVariable = false;// 让VS Code发送setVariable请求
		response.body.supportsSetExpression = false;// 让VS Code发送setExpression请求

		// 使VS Code发送反汇编请求
		response.body.supportsDisassembleRequest = false;
		response.body.supportsSteppingGranularity = false;
		response.body.supportsInstructionBreakpoints = false;

		// 使VS Code能够读写变量内存
		response.body.supportsReadMemoryRequest = true;
		response.body.supportsWriteMemoryRequest = false;

		response.body.supportSuspendDebuggee = true;//调试适配器支持断开连接请求上的suspendDebuggee属性。
		response.body.supportTerminateDebuggee = true;//调试适配器支持断开连接请求上的terminateDebuggee属性。
		response.body.supportsFunctionBreakpoints = true;

		this.sendResponse(response);

		// 因为这个调试适配器可以在任何时候接受像'setBreakpoint'这样的配置请求，
		// 我们通过向前端发送'initializeRequest'来提前请求它们。
		// 前端将通过调用'configurationDone'请求结束配置序列。
		this.sendEvent(new InitializedEvent());
	}
	protected configurationDoneRequest(response: DebugProtocol.ConfigurationDoneResponse, args: DebugProtocol.ConfigurationDoneArguments, request?: DebugProtocol.Request | undefined): void {
		super.configurationDoneRequest(response, args);
		this._configurationDone.notify();
	}
	protected disconnectRequest(response: DebugProtocol.DisconnectResponse, args: DebugProtocol.DisconnectArguments, request?: DebugProtocol.Request): void {
		this.socket.close();
	}

	protected async attachRequest(response: DebugProtocol.AttachResponse, args: IAttachRequestArguments) {
		return this.launchRequest(response, args);
	}

	protected async launchRequest(response: DebugProtocol.LaunchResponse, args: ILaunchRequestArguments) {
		this._configurationDone.wait(1000);
		this.sendResponse(response);
		if(args.libraryName) {
			this.libraryName=args.libraryName;
		}else{
			var result=await vscode.window.showInputBox({
				title:"输入当前程序集名"
			});
			if(result)this.libraryName=result;
			else{
				vscode.window.showErrorMessage("必须输入程序集名称才能继续调试");
				this.sendEvent(new TerminatedEvent());
			}
		}
		if(args.projectPath) {
			this.projectPath=args.projectPath.replace(/\\/g,'/');
		}else{
			vscode.window.showErrorMessage("获取当前工程目录失败");
			this.sendEvent(new TerminatedEvent());
		}
	}

	protected async setBreakPointsRequest(response: DebugProtocol.SetBreakpointsResponse, args: DebugProtocol.SetBreakpointsArguments): Promise<void> {

		var path = this.getLocalPath(args.source.path as string);
		const clientLines = args.lines || [];

		var sbuf=new RainBufferGenerator();
		sbuf.pushString(path);
		sbuf.pushInt32(clientLines.length);
		for(var i=0;i<clientLines.length;i++){
			sbuf.pushInt32(clientLines[i]);
		}
		var reqRes=await this.Request(SCProto.ClearBreakpoint,sbuf);
		if(reqRes){
			var cnt=reqRes.readInt32();
			const bps:DebugProtocol.Breakpoint[]=[];
			while(cnt-->0){
				bps.push({
					id:reqRes.readInt32(),
					line:reqRes.readInt32(),
					verified:reqRes.readBool(),
				})
			}
			response.body={
				breakpoints:bps
			};
		}else{
			response.body={
				breakpoints:[]
			}
		}
		this.sendResponse(response);
	}

	protected breakpointLocationsRequest(response: DebugProtocol.BreakpointLocationsResponse, args: DebugProtocol.BreakpointLocationsArguments, request?: DebugProtocol.Request): void {

		if (args.source.path) {
			response.body = {
				breakpoints: [
					{
						line:args.line,
						column:0
					}
				]
				};
		} else {
			response.body = {
				breakpoints: []
			};
		}
		this.sendResponse(response);
	}

	protected async setExceptionBreakPointsRequest(response: DebugProtocol.SetExceptionBreakpointsResponse, args: DebugProtocol.SetExceptionBreakpointsArguments, request?: DebugProtocol.Request | undefined): Promise<void> {
		var filter="0";
		if(args.filterOptions){
			args.filterOptions.forEach(element => {
				if(element.filterId=='退出码'){
					if(element.condition){
						filter=element.condition;
					}
				}
			});
		}
		var buf=new RainBufferGenerator();
		buf.pushString(filter);
		await this.Request(SCProto.SetExceptionFilter,buf);
		this.sendResponse(response);
	}

	protected exceptionInfoRequest(response: DebugProtocol.ExceptionInfoResponse, args: DebugProtocol.ExceptionInfoArguments) {
		var exc=this.exceptions.get(args.threadId);
		if(exc){
			response.body = {
				exceptionId: exc.code.toString(),
				description: exc.text,
				breakMode: 'always',
				details: {
					message: exc.text,
					typeName: exc.code.toString(),
					stackTrace: exc.stack,
				}
			};
		}
		this.sendResponse(response);
	}

	protected async threadsRequest(response: DebugProtocol.ThreadsResponse): Promise<void> {
		var result = await this.Request(SCProto.GetCoroutines);
		if(result){
			var cnt=result.readInt32();
			const threads:Thread[]=[]
			while(cnt-->0){
				var id=result.readInt32();
				threads.push({
					id:id,
					name:"协程ID "+id.toString()
				})
			}
			response.body={
				threads:threads,
			};
		}else{
			response.body={threads:[]};
		}
		this.sendResponse(response);
	}

	protected async stackTraceRequest(response: DebugProtocol.StackTraceResponse, args: DebugProtocol.StackTraceArguments): Promise<void> {
		var buf=new RainBufferGenerator()
		buf.pushInt32(args.threadId);
		var result=await this.Request(SCProto.GetStack,buf);
		const stks:DebugProtocol.StackFrame[]=[];
		if(result){
			var cnt = result.readInt32();
			while(cnt-->0){
				stks.push({
					id : cnt,
					name : result.readString(),
					line : result.readInt32(),
					source:{
						path : this.projectPath + result.readString(),
					},
					column : 0
				});
			}
		}
		response.body = {
			stackFrames: stks,
			totalFrames: stks.length
		};
		this.sendResponse(response);
	}

	protected async scopesRequest(response: DebugProtocol.ScopesResponse, args: DebugProtocol.ScopesArguments): Promise<void> {
		response.body = {
			scopes: [
				new Scope("局部变量", 0x7fff_ffff, false),
				new Scope("程序集", 0x7fff_fffe, true)
			]
		};
		this.sendResponse(response);
	}

	protected async variablesRequest(response: DebugProtocol.VariablesResponse, args: DebugProtocol.VariablesArguments, request?: DebugProtocol.Request): Promise<void> {
		let vs:DebugProtocol.Variable[]=[]
		var buf=new RainBufferGenerator()
		buf.pushInt32(args.variablesReference);
		var result=await this.Request(SCProto.GetVariable,buf);
		if(result){
			var cnt=result.readInt32();
			while(cnt-->0){
				vs.push({
					name : result.readString(),
					variablesReference : result.readInt32(),
					value : "",
				});
			}
			cnt=result.readInt32();
			while(cnt-->0){
				vs.push({
					name : result.readString(),
					variablesReference : result.readInt32(),
					type : result.readString(),
					value : result.readString(),
				});
			}
		}

		response.body = {
			variables: vs
		};
		this.sendResponse(response);
	}

	protected async continueRequest(response: DebugProtocol.ContinueResponse, args: DebugProtocol.ContinueArguments): Promise<void> {
		await this.Request(SCProto.Continue);
		this.sendResponse(response);
	}

	protected async nextRequest(response: DebugProtocol.NextResponse, args: DebugProtocol.NextArguments): Promise<void> {
		await this.Request(SCProto.Next);
		this.sendResponse(response);
	}
	protected async stepInRequest(response: DebugProtocol.StepInResponse, args: DebugProtocol.StepInArguments, request?: DebugProtocol.Request | undefined): Promise<void> {
		await this.Request(SCProto.Next);
		this.sendResponse(response);
	}
	protected stepOutRequest(response: DebugProtocol.StepOutResponse, args: DebugProtocol.StepOutArguments, request?: DebugProtocol.Request | undefined): void {
		this.sendEvent(new StoppedEvent("不支持跳出",args.threadId));
		this.sendResponse(response);
	}
	protected async pauseRequest(response: DebugProtocol.PauseResponse, args: DebugProtocol.PauseArguments, request?: DebugProtocol.Request | undefined): Promise<void> {
		await this.Request(SCProto.Pause);
		this.sendResponse(response);
	}
	protected async evaluateRequest(response: DebugProtocol.EvaluateResponse, args: DebugProtocol.EvaluateArguments, request?: DebugProtocol.Request | undefined): Promise<void> {
		if(args.context=='hover'){
			var splits = args.expression.split(' ');
			if(splits.length==3){
				var path=splits[0];
				var line=Number.parseInt(splits[1]);
				var col=Number.parseInt(splits[2]);
				var buf=new RainBufferGenerator();
				buf.pushString(this.getLocalPath(path));
				buf.pushInt32(line);
				buf.pushInt32(col);
				var result = await this.Request(SCProto.GetHover,buf);
				if(result && result.readBool()){
					response.body={
						variablesReference : result.readInt32(),
						result : result.readString(),
					};
				}
			}
		}
		this.sendResponse(response);
	}
	private getLocalPath(path:string):string{
		return path.replace(/\\/g,'/').substring(this.projectPath.length); 
	}
}

