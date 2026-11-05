#!/usr/bin/env node
import { TRANSFORMATIONS, transformText } from "./index.js";

const [operation, ...textParts] = process.argv.slice(2);

if (operation === "--help" || operation === "-h") {
  printUsage();
  process.exit(0);
}

if (!operation || !Object.hasOwn(TRANSFORMATIONS, operation)) {
  printUsage();
  process.exitCode = 1;
} else if (textParts.length > 0) {
  console.log(transformText(textParts.join(" "), operation));
} else if (process.stdin.isTTY) {
  printUsage();
  process.exitCode = 1;
} else {
  let input = "";
  for await (const chunk of process.stdin) {
    input += chunk;
  }
  process.stdout.write(transformText(input, operation));
}

function printUsage() {
  console.error(
    [
      "Usage: caps-unlock <operation> [text]",
      "",
      `Operations: ${Object.keys(TRANSFORMATIONS).join(", ")}`,
      "If text is omitted, input is read from stdin.",
    ].join("\n"),
  );
}
