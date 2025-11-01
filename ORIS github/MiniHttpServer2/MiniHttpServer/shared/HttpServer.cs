using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.shared
{
    internal class HttpServer
    {
        private readonly HttpListener _listener = new();
        private readonly SettingsManager _settingsManager = SettingsManager.Instance;

        public HttpServer()
        {
            try
            {
                InitConsoleInput();
                Console.WriteLine($"Настройки загружены: {_settingsManager.Domain}:{_settingsManager.Port}, папка: {_settingsManager.PublicDirectoryPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Критическая ошибка при инициализации сервера: " + ex);
                Environment.Exit(1);
            }
        }

        private void InitConsoleInput()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var input = Console.ReadLine();
                        if (input == null) continue;

                        if (input.Trim().ToLower() == "/stop")
                        {
                            Console.WriteLine("Остановка сервера...");
                            Stop();
                            Environment.Exit(0);
                        }
                        else if (input.Trim().ToLower() == "/reload")
                        {
                            _settingsManager.ReloadSettings();
                            Console.WriteLine("Настройки перезагружены");
                            Console.WriteLine($"Обновленные настройки: {_settingsManager.Domain}:{_settingsManager.Port}, папка: {_settingsManager.PublicDirectoryPath}");
                        }
                        else if (!string.IsNullOrEmpty(input))
                        {
                            Console.WriteLine("Неизвестная команда. Доступные команды:");
                            Console.WriteLine("  /stop   - остановить сервер");
                            Console.WriteLine("  /reload - перезагрузить настройки");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка при чтении ввода: " + ex.Message);
                    }
                }
            });
        }

        public void Start()
        {
            try
            {
                _listener.Prefixes.Add($"http://{_settingsManager.Domain}:{_settingsManager.Port}/");
                _listener.Start();
                Console.WriteLine($"Сервер запущен на http://{_settingsManager.Domain}:{_settingsManager.Port}/");

                _ = ListenLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при запуске сервера: " + ex);
                Environment.Exit(1);
            }
        }

        public void Stop()
        {
            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                    Console.WriteLine("Сервер остановлен");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при остановке сервера: " + ex.Message);
            }
        }

        private async Task ListenLoop()
        {
            while (true)
            {
                try
                {
                    if (!_listener.IsListening) break;

                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при приеме запроса: " + ex);
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            string relativePath = context.Request.Url.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(relativePath))
                relativePath = "index.html";

            string filePath = Path.Combine(_settingsManager.PublicDirectoryPath, relativePath);

            string requestSignalizer = filePath.Split(".")[1];
            if (requestSignalizer == "html")
                Console.WriteLine("Получен запрос!");

            try
            {
                if (!File.Exists(filePath))
                {
                    context.Response.StatusCode = 404;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    byte[] notFound = Encoding.UTF8.GetBytes("<h1>404 Not Found</h1>");
                    context.Response.ContentLength64 = notFound.Length;
                    await context.Response.OutputStream.WriteAsync(notFound);
                    context.Response.Close();
                    return;
                }

                string contentType = GetContentType(Path.GetExtension(filePath));
                context.Response.ContentType = contentType;

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                context.Response.ContentLength64 = fileBytes.Length;
                await context.Response.OutputStream.WriteAsync(fileBytes);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[500] Ошибка при обработке {relativePath}: {ex.Message}");

                try
                {
                    context.Response.StatusCode = 500;
                    byte[] buffer = Encoding.UTF8.GetBytes("<h1>500 Internal Server Error</h1>");
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer);
                }
                catch { }
            }
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".html" => "text/html; charset=utf-8",
                ".htm" => "text/html; charset=utf-8",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                ".txt" => "text/plain; charset=utf-8",
                _ => "application/octet-stream"
            };
        }
    }
}
