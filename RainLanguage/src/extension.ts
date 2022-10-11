
import * as vscode from "vscode";
import FormatProvider from './formatterProvider' 


export async function activate(context:vscode.ExtensionContext) {
    const documentSelector: vscode.DocumentSelector = {
        language: '雨言',
    };
    context.subscriptions.push(vscode.languages.registerDocumentRangeFormattingEditProvider(documentSelector, 
        new FormatProvider()
        ));
    context.subscriptions.push(vscode.languages.registerOnTypeFormattingEditProvider(documentSelector, 
        new FormatProvider(), 
        '\n'));
}
export async function deactivate() {
}