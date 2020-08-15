# JsonnetBinding-Dotnet

.NET Bindings for [Jsonnet](https://jsonnet.org/).

Evaluating a file.

```
var result = new JsonnetVm().EvaluateFile("test.jsonnet");
```

Evaluating a snippet.

```
var result = new JsonnetVm().EvaluateSnippet(
    "test.jsonnet",
    "{ x: 1 , y: self.x + 1 } { x: 10 }"
);
```
