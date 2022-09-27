/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/
'use strict';

import * as vscode from 'vscode';
import * as rainBase from './rainBase'
import { RainDebugSession } from './rainDebug';

class InlineDebugAdapterFactory implements vscode.DebugAdapterDescriptorFactory {

	createDebugAdapterDescriptor(_session: vscode.DebugSession): vscode.ProviderResult<vscode.DebugAdapterDescriptor> {
		return new vscode.DebugAdapterInlineImplementation(new RainDebugSession());
	}
}

export function activate(context: vscode.ExtensionContext) {
	context.subscriptions.push(
		vscode.commands.registerCommand('雨言调试器.运行', (resource: vscode.Uri) => {
			let targetResource = resource;
			if (!targetResource && vscode.window.activeTextEditor) {
				targetResource = vscode.window.activeTextEditor.document.uri;
			}
			if (targetResource) {
				vscode.debug.startDebugging(undefined, {
					type: '雨言',
					name: '运行当前工作区',
					request: 'launch',
					program: targetResource.fsPath
				},
					{ noDebug: true }
				);
			}
		}),
		vscode.commands.registerCommand('雨言调试器.远程调试', (resource: vscode.Uri) => {
			let targetResource = resource;
			if (!targetResource && vscode.window.activeTextEditor) {
				targetResource = vscode.window.activeTextEditor.document.uri;
			}
			if (targetResource) {
				vscode.debug.startDebugging(undefined, {
					type: '雨言',
					name: '远程调试',
					request: 'launch',
					program: targetResource.fsPath,
					stopOnEntry: true
				});
			}
		}),
		vscode.debug.registerDebugAdapterDescriptorFactory('雨言',new InlineDebugAdapterFactory()),
		vscode.debug.registerDebugConfigurationProvider('雨言',{
			resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, config: vscode.DebugConfiguration, token?: vscode.CancellationToken): vscode.ProviderResult<vscode.DebugConfiguration>{
				if(!config.libraryName){
					config.libraryName="${workspaceFolderBasename}";
				}
				config.projectPath="${workspaceFolder}/";
				return config;
			}
		}),
		// vscode.languages.registerEvaluatableExpressionProvider('雨言',{
		// 	provideEvaluatableExpression(document: vscode.TextDocument, position: vscode.Position): vscode.ProviderResult<vscode.EvaluatableExpression> {
				
		// 		console.log("开始匹配");
		// 		const reg = /[a-z][a-z0-9_]*/ig;
		// 		const line = document.lineAt(position.line).text;
		// 		let match : RegExpExecArray | null;
		// 		while(match = reg.exec(line)){
		// 			var range = new vscode.Range(position.line, match.index, position.line, match.index + match[0].length);
		// 			if(range.contains(position)){
		// 				var expr = position.line+" "+position.character;
		// 				console.log("expr:"+expr);
		// 				return new vscode.EvaluatableExpression(range,expr);
		// 			}
		// 		}
		// 		return undefined;
		// 	},
		// })
	);
	rainBase.activate();
}

export function deactivate() {
	rainBase.deactivate();
}