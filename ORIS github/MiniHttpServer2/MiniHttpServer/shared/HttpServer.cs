using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.shared
{
    internal class HttpServer
    {
        private readonly HttpListener _listener = new();
        private SettingsModel _config;

        public HttpServer()
        {
            try
            {
                LoadConfig();
                InitConsoleInput();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Критическая ошибка при инициализации сервера: " + ex);
                Environment.Exit(1);
            }
        }

        private void LoadConfig()
        {
            try
            {
                string settingsJson = File.ReadAllText("settings.json");
                _config = JsonSerializer.Deserialize<SettingsModel>(settingsJson);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Файл settings.json не найден");
                Environment.Exit(1);
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Ошибка в формате JSON: " + ex.Message);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Неожиданная ошибка при загрузке настроек: " + ex);
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
                        else if (!string.IsNullOrEmpty(input))
                        {
                            Console.WriteLine("Неизвестная команда. Введите /stop для остановки сервера");
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
                _listener.Prefixes.Add($"http://{_config.Domain}:{_config.Port}/");
                _listener.Start();
                Console.WriteLine($"Сервер запущен на http://{_config.Domain}:{_config.Port}/");

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
                    // Сервер остановлен выходим из цикла
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Listener был уничтожен
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
            try
            {
                byte[] responseFile = [];
                var request = context.Request;
                var response = context.Response;

                try
                {
                    string path = Path.Combine(_config.PublicDirectoryPath + request.Url.AbsolutePath);
                    responseFile = await File.ReadAllBytesAsync(path);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"Файл {request.Url.AbsolutePath} не найден");
                    //responseFile = "<h1>404 Not Found</h1>";
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine("Указан неверный путь к директории");
                    //responseFile = "<h1>500 Internal Server Error</h1>";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при чтении файла: " + ex);
                    //responseFile = "<h1>500 Internal Server Error</h1>";
                }

                byte[] buffer = responseFile;
                response.ContentLength64 = buffer.Length;

                using Stream output = response.OutputStream;
                await output.WriteAsync(buffer);
                await output.FlushAsync();

                Console.WriteLine($"Запрос от {context.Request.RemoteEndPoint} обработан");
            }

            catch (Exception ex)
            {
                Console.WriteLine("Критическая ошибка при обработке запроса: " + ex);

                try
                {
                    var response = context.Response;
                    byte[] buffer = Encoding.UTF8.GetBytes("<h1>500 Internal Server Error</h1>");
                    response.ContentLength64 = buffer.Length;
                    using Stream output = response.OutputStream;
                    await output.WriteAsync(buffer);
                }
                catch
                {

                }
            }
        }
    }
}
