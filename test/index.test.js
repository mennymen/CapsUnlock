import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  collapseWhitespace,
  camelCase,
  invertCase,
  kebabCase,
  lowerCase,
  pascalCase,
  removeBlankLines,
  sentenceCase,
  snakeCase,
  titleCase,
  transformText,
  trim,
  upperCase,
} from "../src/index.js";

test("upperCase and lowerCase convert Unicode text", () => {
  assert.equal(upperCase("Straße"), "STRASSE");
  assert.equal(lowerCase("HELLO"), "hello");
});

test("invertCase swaps cased characters and preserves uncased characters", () => {
  assert.equal(invertCase("Hello, 123!"), "hELLO, 123!");
  assert.equal(invertCase("Été"), "éTÉ");
});

test("titleCase capitalizes words and lowercases the rest", () => {
  assert.equal(titleCase("  hELLO, WORLD! it's NICE. "), "  Hello, World! It's Nice. ");
  assert.equal(titleCase("добрый ДЕНЬ"), "Добрый День");
});

test("sentenceCase capitalizes sentence starts", () => {
  assert.equal(sentenceCase("hELLO WORLD. HOW ARE YOU?"), "Hello world. How are you?");
  assert.equal(sentenceCase("already lower"), "Already lower");
  assert.equal(sentenceCase("first line\nSECOND LINE"), "First line\nSecond line");
});

test("developer naming styles split camel case and acronyms", () => {
  const input = "HTTPServer myURLValue";
  assert.equal(camelCase(input), "httpServerMyUrlValue");
  assert.equal(pascalCase(input), "HttpServerMyUrlValue");
  assert.equal(snakeCase(input), "http_server_my_url_value");
  assert.equal(kebabCase(input), "http-server-my-url-value");
});

test("trim and collapseWhitespace handle surrounding and repeated spaces", () => {
  assert.equal(trim(" \t hello \n"), "hello");
  assert.equal(collapseWhitespace(" \t hello \n world  "), " hello world ");
  assert.equal(removeBlankLines("first\r\n\r\n  \r\nsecond\n"), "first\r\nsecond\n");
});

test("transformText dispatches supported operations and rejects unknown ones", () => {
  assert.equal(transformText("hello", "uppercase"), "HELLO");
  assert.equal(transformText("Hello World", "camel"), "helloWorld");
  assert.equal(transformText("Hello World", "pascal"), "HelloWorld");
  assert.equal(transformText("Hello World", "snake"), "hello_world");
  assert.equal(transformText("Hello World", "kebab"), "hello-world");
  assert.equal(transformText("one\n\n two", "remove-blank-lines"), "one\n two");
  assert.throws(() => transformText("hello", "unknown"), RangeError);
});

test("CLI transforms command-line text and reads piped input", () => {
  const cliPath = fileURLToPath(new URL("../src/cli.js", import.meta.url));
  const fromArguments = spawnSync(
    process.execPath,
    [cliPath, "title", "hello", "world"],
    { encoding: "utf8" },
  );
  assert.equal(fromArguments.status, 0);
  assert.equal(fromArguments.stdout, "Hello World\n");

  const fromStdin = spawnSync(process.execPath, [cliPath, "uppercase"], {
    encoding: "utf8",
    input: "hello",
  });
  assert.equal(fromStdin.status, 0);
  assert.equal(fromStdin.stdout, "HELLO");

  const invalidOperation = spawnSync(process.execPath, [cliPath, "unknown", "hello"], {
    encoding: "utf8",
  });
  assert.equal(invalidOperation.status, 1);
  assert.match(invalidOperation.stderr, /Usage: caps-unlock/);
});
