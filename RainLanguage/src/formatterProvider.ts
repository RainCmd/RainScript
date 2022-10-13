
import { DocumentRangeFormattingEditProvider, FormattingOptions, CancellationToken, TextEdit, TextDocument, Range, Position, ProviderResult } from 'vscode';

export default class FormatProvider implements DocumentRangeFormattingEditProvider {
    public async provideDocumentRangeFormattingEdits(document: TextDocument, range: Range, options: FormattingOptions, token: CancellationToken): Promise<TextEdit[]> {
        let results: TextEdit[] = [];

        for (let index = range.start.line; index <= range.end.line; index++) {
            const line = document.lineAt(index);
            const newText = this.Format(line.text);
            if (line.text != newText) {
                results.push(new TextEdit(line.range, newText));
            }
        }
        return results;
    }
    public async provideOnTypeFormattingEdits(document: TextDocument, position: Position, ch: string, options: FormattingOptions, token: CancellationToken): Promise<TextEdit[]> {
        const line = document.lineAt(position.line - 1);
        const newText = this.Format(line.text);
        if (line.text != newText) {
            return [
                new TextEdit(line.range, this.Format(line.text))
            ];
        } else {
            return [];
        }
    }
    private Format(line: string): string {
        line = line.replace(/(?<=[\)\]\}]|\b)\s*(?=[&|^<>=\+\-*/%!`?:])/gi, " ");
        line = line.replace(/(?<=[&|^<>=\+\-*/%?])\s*(?=[\(\[\{\.]|\b)/gi, " ");
        line = line.replace(/(?<=[:,])\s*/gi, " ");

        line = line.replace(/\b\s*(?=[,\)\]\}\.\{\[\(]|\+\+|\-\-)/gi, "");
        line = line.replace(/(?<=[\(\[\{\.!`]|\+\+|\-\-)\s*\b/gi, "");
        line = line.replace(/(?<=[\)\]\}]|\+\+|\-\-)\s*(?=[\.\(\[\{])/gi, "");
        line = line.replace(/(?<=[&|^<>=*/%?])\s*([\+\-])\s*\b/gi, " $1");
        line = line.replace(/([\+\-])\s+([\+\-])\s*\b/gi, "$1 $2");
        return line;
    }
}