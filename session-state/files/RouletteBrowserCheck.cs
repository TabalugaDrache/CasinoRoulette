using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var profile = Path.GetFullPath(@"session-state\files\roulette-browser-profile");
using var edge = Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")
{
    Arguments = $"--headless=new --disable-gpu --no-first-run --no-default-browser-check --remote-debugging-port=9337 --user-data-dir=\"{profile}\" about:blank",
    UseShellExecute = false
}) ?? throw new Exception("Edge could not start.");
try
{
    using var http = new HttpClient();
    string? endpoint = null;
    for (var attempt = 0; attempt < 60 && endpoint is null; attempt++)
    {
        try
        {
            using var targets = JsonDocument.Parse(await http.GetStringAsync("http://127.0.0.1:9337/json/list"));
            endpoint = targets.RootElement.EnumerateArray().FirstOrDefault(t => t.GetProperty("type").GetString() == "page").GetProperty("webSocketDebuggerUrl").GetString();
        }
        catch (HttpRequestException) { await Task.Delay(250); }
    }
    using var socket = new ClientWebSocket();
    await socket.ConnectAsync(new Uri(endpoint ?? throw new Exception("CDP unavailable.")), CancellationToken.None);
    var id = 0;
    async Task<JsonElement> Send(string method, object parameters)
    {
        var current = ++id;
        await socket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { id = current, method, @params = parameters })), WebSocketMessageType.Text, true, CancellationToken.None);
        var buffer = new byte[65536];
        while (true)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                message.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
            using var doc = JsonDocument.Parse(message.ToArray());
            if (doc.RootElement.TryGetProperty("id", out var responseId) && responseId.GetInt32() == current)
            {
                if (doc.RootElement.TryGetProperty("error", out var error)) throw new Exception(error.ToString());
                return doc.RootElement.GetProperty("result").Clone();
            }
        }
    }
    async Task<JsonElement> Eval(string expression)
    {
        var response = await Send("Runtime.evaluate", new { expression, returnByValue = true, awaitPromise = true });
        if (response.TryGetProperty("exceptionDetails", out var error)) throw new Exception(error.ToString());
        return response.GetProperty("result").TryGetProperty("value", out var value) ? value.Clone() : default;
    }
    async Task Wait(string expression)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if ((await Eval(expression)).ValueKind == JsonValueKind.True) return;
            await Task.Delay(100);
        }
        throw new Exception("Timed out: " + expression);
    }
    async Task Check(string name, string expression)
    {
        if ((await Eval(expression)).ValueKind != JsonValueKind.True) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
    async Task Viewport(int width, int height)
    {
        await Send("Emulation.setDeviceMetricsOverride", new { width, height, deviceScaleFactor = 1, mobile = false });
        await Task.Delay(200);
        Console.WriteLine($"Viewport {width}x{height}: " + await Eval("JSON.stringify({height:innerHeight,documentHeight:document.documentElement.scrollHeight,width:innerWidth,documentWidth:document.documentElement.scrollWidth})"));
        await Check($"No page overflow at {width}x{height}", "document.documentElement.scrollHeight <= innerHeight && document.documentElement.scrollWidth <= innerWidth");
        await Check("Footer visible", "document.querySelector('.page-footer').getBoundingClientRect().bottom <= innerHeight");
    }
    await Send("Emulation.setDeviceMetricsOverride", new { width = 1366, height = 768, deviceScaleFactor = 1, mobile = false });
    await Send("Page.navigate", new { url = "http://127.0.0.1:5187" });
    await Wait("!!document.querySelector('#roulette-number') && !document.querySelector('#roulette-number').disabled");
    await Eval("localStorage.removeItem('roulette-journal-spins-v1'); location.reload()");
    await Wait("!!document.querySelector('#roulette-number') && !document.querySelector('#roulette-number').disabled");
    await Viewport(1024, 640);
    async Task Enter(string value)
    {
        await Eval($"(() => {{ const input = document.querySelector('#roulette-number'); input.focus(); input.value = {JsonSerializer.Serialize(value)}; input.dispatchEvent(new Event('input', {{bubbles:true}})); }})()");
        await Task.Delay(120);
        await Send("Input.dispatchKeyEvent", new { type = "keyDown", key = "Enter", code = "Enter", windowsVirtualKeyCode = 13, text = "\r" });
        await Send("Input.dispatchKeyEvent", new { type = "keyUp", key = "Enter", code = "Enter", windowsVirtualKeyCode = 13 });
        await Task.Delay(200);
    }
    foreach (var invalid in new[] { "", "37", "-1", "1.5", "abc" })
    {
        await Enter(invalid);
        await Wait("document.querySelector('#roulette-number').getAttribute('aria-invalid') === 'true'");
        await Check("Invalid value rejected: " + invalid, "localStorage.getItem('roulette-journal-spins-v1') === null");
    }
    foreach (var valid in new[] { 0, 36, 7, 7, 12, 12, 12, 1, 2, 3, 4, 5 }) await Enter(valid.ToString());
    await Wait("JSON.parse(localStorage.getItem('roulette-journal-spins-v1') || '[]').length === 12");
    await Check("Enter saves correct sequence", "JSON.stringify(JSON.parse(localStorage.getItem('roulette-journal-spins-v1'))) === '[0,36,7,7,12,12,12,1,2,3,4,5]'");
    await Check("Input cleared and focus retained", "document.querySelector('#roulette-number').value === '' && document.activeElement.id === 'roulette-number'");
    await Check("Statistics updated", "document.querySelectorAll('.summary-value')[2].textContent.trim() === '12'");
    await Check("History limited to ten", "document.querySelectorAll('.history-list .number-ball').length === 10");
    await Viewport(1024, 640);
    await Viewport(1366, 768);
    await Eval("Array.from(document.querySelectorAll('button')).find(b => b.textContent.trim() === 'ÄLTERE').click()");
    await Wait("document.querySelectorAll('.history-list .number-ball').length === 2");
    await Check("Older page reachable", "document.querySelector('.history-list').textContent.trim() === '360'");
    await Eval("location.reload()");
    await Wait("!!document.querySelector('#roulette-number') && !document.querySelector('#roulette-number').disabled");
    await Check("Persistence after reload", "document.querySelector('.count-badge').textContent === '12'");
    await Eval("Array.from(document.querySelectorAll('button')).find(b => b.textContent.trim() === 'VERLAUF LÖSCHEN').click()");
    await Wait("!!document.querySelector('.confirm-clear')");
    await Viewport(1024, 640);
    await Eval("document.querySelector('.confirm-clear button').click()");
    await Wait("document.querySelector('.count-badge').textContent === '0'");
    await Check("Clear persisted", "localStorage.getItem('roulette-journal-spins-v1') === '[]'");
    await Eval("localStorage.setItem('roulette-journal-spins-v1', JSON.stringify(Array.from({length:370}, (_,i) => i % 37))); location.reload()");
    await Wait("!!document.querySelector('#roulette-number') && !document.querySelector('#roulette-number').disabled");
    await Viewport(1024, 640);
    await Check("Ties remain compact", "document.querySelectorAll('.summary-value')[2].textContent.trim() === '0' && document.querySelectorAll('.summary-foot')[2].textContent.includes('37 gleichauf')");
    await Send("Emulation.setDeviceMetricsOverride", new { width = 390, height = 844, deviceScaleFactor = 1, mobile = false });
    await Task.Delay(150);
    await Check("Mobile has no horizontal overflow", "document.documentElement.scrollWidth <= innerWidth");
    Console.WriteLine("All browser checks passed.");
}
finally
{
    if (!edge.HasExited) edge.Kill(entireProcessTree: true);
}
