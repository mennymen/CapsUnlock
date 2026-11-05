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

CapsUnlock is a tiny text-transformation library, a command-line tool, and a
Windows utility for transforming selected text in place.

## Get CapsUnlock for Windows

**[Download the latest Windows installer](https://github.com/mennymen/CapsUnlock/releases/latest/download/CapsUnlock-Setup-0.1.0.exe).**

Run the installer once. CapsUnlock launches after setup and starts automatically
whenever you sign in. It installs just for your Windows account, needs no
administrator access or Node.js, and can be removed from **Settings → Apps**.

The app stays in the notification area while it runs. Select text in a supported
app and press one of the shortcuts above. CapsUnlock preserves multi-line
selections and never reads, changes, or clears the clipboard. Exit from the
CapsUnlock notification-area icon's menu.

> **Compatibility:** Apps must expose editable text selections through Windows
> UI Automation. Unsupported controls show a notification and remain unchanged.
> A shortcut may also be unavailable if another program already uses it.

## Try the library

Install the package from npm:

```sh
npm install caps-unlock
```

```js
import { invertCase, sentenceCase, titleCase, transformText } from "caps-unlock";

invertCase("Hello, 123!");                     // "hELLO, 123!"
titleCase("hELLO, WORLD!");                    // "Hello, World!"
sentenceCase("hELLO. HOW ARE YOU?");            // "Hello. How are you?"
transformText("hello", "uppercase");            // "HELLO"
```

Available functions: `upperCase`, `lowerCase`, `invertCase`, `titleCase`,
`sentenceCase`, `trim`, and `collapseWhitespace`. `transformText` accepts
`uppercase`, `lowercase`, `invert`, `title`, `sentence`, `trim`, or
`collapse-whitespace`.

The case transformations support Unicode. Title case capitalizes each word and
lowercases the remaining letters. Sentence case lowercases the text and
capitalizes its first letter and letters after `.`, `!`, or `?` followed by
whitespace. `collapseWhitespace` turns each run of whitespace into one space
without trimming the ends.

## Use the command line

Pass text directly or pipe it in:

```sh
npx caps-unlock title "hELLO, WORLD!"
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

The installer is written to `dist\CapsUnlock-Setup-0.1.0.exe`. To run the
Windows utility directly while developing:

```powershell
dotnet run --project .\windows\CapsUnlock.Hotkeys\CapsUnlock.Hotkeys.csproj
```
