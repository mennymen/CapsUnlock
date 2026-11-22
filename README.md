# CapsUnlock

**Fix the case of selected text from anywhere on Windows.** No retyping, no
clipboard changes, and no need to leave the app you're working in.

Select text, press a shortcut, and keep typing:

| Shortcut | Result |
| --- | --- |
| `Ctrl` + `Shift` + `U` | `make this uppercase` → `MAKE THIS UPPERCASE` |
| `Ctrl` + `Shift` + `L` | `MAKE THIS LOWERCASE` → `make this lowercase` |
| `Ctrl` + `Shift` + `I` | `Hello World!` → `hELLO wORLD!` |
| `Ctrl` + `Shift` + `T` | `hELLO, WORLD!` → `Hello, World!` |
| `Ctrl` + `Shift` + `S` | `hello. HOW ARE YOU?` → `Hello. How are you?` |
| `Ctrl` + `Shift` + `C` | `Hello World` → `helloWorld` |
| `Ctrl` + `Shift` + `P` | `hello world` → `HelloWorld` |
| `Ctrl` + `Shift` + `N` | `Hello World` → `hello_world` |
| `Ctrl` + `Shift` + `K` | `Hello World` → `hello-world` |
| `Ctrl` + `Shift` + `R` | Trim whitespace around the selected text |
| `Ctrl` + `Shift` + `W` | Collapse whitespace runs to single spaces |
| `Ctrl` + `Shift` + `B` | Remove blank lines from the selection |

CapsUnlock is a tiny text-transformation library, a command-line tool, and a
Windows utility for transforming selected text in place.

## Get CapsUnlock for Windows

**[Download the latest Windows installer](https://github.com/mennymen/CapsUnlock/releases/latest/download/CapsUnlock-Setup-0.2.1.exe).**

Run the installer once. CapsUnlock launches after setup and starts automatically
whenever you sign in. It installs just for your Windows account, needs no
administrator access or Node.js, and can be removed from **Settings → Apps**.

The app stays in the notification area while it runs. **For safety, text
replacement is currently supported only in Windows Notepad.** Outlook, Teams,
WhatsApp, and other apps are left unchanged until their rich-text editors can be
handled without risking formatting, images, or other embedded content.
CapsUnlock preserves multi-line Notepad selections and never reads, changes, or
clears the clipboard. Exit from the CapsUnlock notification-area icon's menu.

Right-click the notification-area icon and choose **Settings** to disable
individual shortcuts or change their modifier and letter keys. Available
modifier combinations are Ctrl+Shift, Ctrl+Alt, Alt+Shift, and Ctrl+Alt+Shift.
CapsUnlock checks for duplicate shortcuts and reports combinations already used
by another app.

> **Compatibility:** Only Notepad's editable text control is currently enabled.
> Other apps show a notification and remain unchanged. A shortcut may also be
> unavailable if another program already uses it.

## Try the library

Install the package from npm:

```sh
npm install caps-unlock
```

```js
import {
  camelCase,
  invertCase,
  kebabCase,
  sentenceCase,
  snakeCase,
  titleCase,
  transformText,
} from "caps-unlock";

invertCase("Hello, 123!");                     // "hELLO, 123!"
titleCase("hELLO, WORLD!");                    // "Hello, World!"
sentenceCase("hELLO. HOW ARE YOU?");            // "Hello. How are you?"
camelCase("HTTP server name");                 // "httpServerName"
snakeCase("HTTP server name");                 // "http_server_name"
kebabCase("HTTP server name");                 // "http-server-name"
transformText("hello", "uppercase");            // "HELLO"
```

Available functions: `upperCase`, `lowerCase`, `invertCase`, `titleCase`,
`sentenceCase`, `camelCase`, `pascalCase`, `snakeCase`, `kebabCase`, `trim`,
`collapseWhitespace`, and `removeBlankLines`. `transformText` accepts
`uppercase`, `lowercase`, `invert`, `title`, `sentence`, `camel`, `pascal`,
`snake`, `kebab`, `trim`, `collapse-whitespace`, or `remove-blank-lines`.

The case transformations support Unicode. Title case capitalizes each word and
lowercases the remaining letters. Sentence case lowercases the text and
capitalizes its first letter and letters after `.`, `!`, or `?` followed by
whitespace. `collapseWhitespace` turns each run of whitespace into one space
without trimming the ends. `removeBlankLines` removes empty or whitespace-only
lines while leaving the other lines and their line endings in place. Naming
styles split camelCase, PascalCase, underscores, hyphens, and acronym runs into
words before joining them in the requested style.

## Use the command line

Pass text directly or pipe it in:

```sh
npx caps-unlock title "hELLO, WORLD!"
npx caps-unlock snake "HTTP server name"
echo "hello world" | npx caps-unlock uppercase
```

Run `caps-unlock --help` to see all operations.

## Build from source

Run the JavaScript tests with Node.js 18 or newer:

```sh
npm test
```

To build the per-user Windows installer on Windows, install the .NET 8 SDK and
[Inno Setup 6](https://jrsoftware.org/isdl.php), then run:

```powershell
.\scripts\build-installer.ps1
```

The installer is written to `dist\CapsUnlock-Setup-0.2.1.exe`. To run the
Windows utility directly while developing:

```powershell
dotnet run --project .\windows\CapsUnlock.Hotkeys\CapsUnlock.Hotkeys.csproj
```
