﻿
using Google.Cloud.Dialogflow.V2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    class Program
    {
        public static TelegramBotClient bot;
        private static string firstName = default;
        private static SessionsClient dFlowClient;
        private static string projectID;
        private static string sessionID;
        private static string path;
        public static Telegram.Bot.Types.Message Message { get; set; }
        //Начальный путь к файлу
        public static string Path
        {
            get { return @"c:\Telegram_Bot_Users"; }
            private set { path = value; }
        }

        /// <summary>
        /// Скачивание файлов
        /// </summary>
        /// <param name="fileId">ID файла отправленное с сервера Telegram</param>
        /// <param name="path">путь куда сохраняется файл</param>
        private static async void Download(string fileId, string path)
        {
            var file = await bot.GetFileAsync(fileId);
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                await bot.DownloadFileAsync(file.FilePath, stream);
            }
        }

        /// <summary>
        /// Отправка файла обратно пользователю
        /// </summary>
        /// <param name="path">Путь где лежит файл</param>
        /// <param name="fileName">имя файла</param>
        /// <param name="messageType">Тип документа (Document, Video, etc..)</param>
        private static async void SendFiles(string path, string fileName, string messageType)
        {

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                switch (messageType)
                {
                    case ("Document"):
                        {
                            Console.WriteLine($"Отправлен файл {fileName}");
                            await bot.SendTextMessageAsync(Message.Chat.Id, "Уже загружаю....");
                            await bot.SendDocumentAsync(Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, fileName));
                            break;
                        }
                    case "Video":
                        {
                            Console.WriteLine($"Отправлен файл {fileName}");
                            await bot.SendTextMessageAsync(Message.Chat.Id, "Уже загружаю....");
                            await bot.SendVideoAsync(Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, fileName));
                            break;
                        }
                    case "Audio":
                        {
                            Console.WriteLine($"Отправлен файл {fileName}");
                            await bot.SendTextMessageAsync(Message.Chat.Id, "Уже загружаю....");
                            await bot.SendAudioAsync(Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, fileName));
                            break;
                        }
                    case "Photo":
                        {
                            Console.WriteLine($"Отправлен файл {fileName}");
                            await bot.SendTextMessageAsync(Message.Chat.Id, "Уже загружаю....");
                            await bot.SendPhotoAsync(Message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, fileName));
                            break;
                        }
                }
            }
        }

        [Obsolete]
        static void Main(string[] args)
        {
            string tokenBot = System.IO.File.ReadAllText(@"C:\Downloads\C#\Token_BOT.txt");
            string dFlowKeyPath = @"C:\Users\Tusen\Dropbox\IKS\C# проекты\C# Учеба\ДЗ 9\TelegramBot\bin\Debug\iksbot-9tan-8bfc6cdbd2be.json";

            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            #region BotAnswerInitiation
            bot = new TelegramBotClient(tokenBot);
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(dFlowKeyPath));
            projectID = dic["project_id"];
            sessionID = dic["private_key_id"];
            var dialogFlowBilder = new SessionsClientBuilder { CredentialsPath = dFlowKeyPath };
            dFlowClient = dialogFlowBilder.Build();
            #endregion

            //Без данной строки Бот не инициируется на сервере
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


            string jsonFile = System.IO.File.ReadAllText("Constellations.json");
            var constParse = JObject.Parse(jsonFile)["Созвездия"].ToArray();
            //данная позиция нужна для метода Substring
            byte fromIndex = 6;
            //данная позиция нужна для метода Substring
            byte numberOfSymbol = 1;

            foreach (var letters in constParse)
            {
                List<Constellation> constellations = new List<Constellation>();
                string letterToString = letters.ToString();
                string letter = letterToString.Substring(fromIndex, numberOfSymbol);
                foreach (var constell in letters[letter])
                {
                    Constellation temp = new Constellation(constell["Name"].ToString(), constell["LatinName"].ToString(),
                                                           constell["UrlMap"].ToString(), constell["UrlPhoto"].ToString(),
                                                           constell["Text"].ToString());

                    constellations.Add(temp);
                }
                LettersConstellations temp2 = new LettersConstellations(letter, constellations);
                LettersConstellations.AlphabetConstellations.Add(temp2);
            }

            bot.OnMessage += BotOnMessage;
            bot.OnCallbackQuery += BotOnCallbackQuery;
            bot.StartReceiving();
            var iAm = bot.GetMeAsync().Result;
            Console.WriteLine(iAm.FirstName);

            Console.ReadLine();
            bot.StopReceiving();

        }

        [Obsolete]
        private static async void BotOnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            string response = e.CallbackQuery.Data;

            //в первое условие попадут данные из метода RequestFiles.ProcessRequestFiles() 
            if (response.Contains("."))
            {
                string[] a = response.Split('.');
                int indexFileName = int.Parse(a[1]);
                string type = a[0];
                string fileName = RequestFiles.FileListName[indexFileName].ToString();

                SendFiles(Path + $@"\{Message.From.FirstName}\{type}\{fileName}", $"{fileName}", type);
                return;
            }
            //во второе условие приходят данные для дальнейшего поиска нужных типов сохраненных файлов
            if (response == MessageType.Document.ToString() || response == MessageType.Video.ToString() ||
                response == MessageType.Photo.ToString() || response == MessageType.Audio.ToString())
                try
                { RequestFiles.ProcessRequestFiles(response, Message.From.FirstName); }
                catch { Console.WriteLine("Ошибка"); }
            //ответ пользователю по запросу о созвездиях
            else
            {
                string[] a = response.Split('|');
                string letter = a[0];
                string constellation = a[1];
                for (int i = 0; i < LettersConstellations.AlphabetConstellations.Count; i++)
                {
                    if (letter == LettersConstellations.AlphabetConstellations[i].Letter)
                    {
                        List<Constellation> temp = LettersConstellations.AlphabetConstellations[i].Constellations;
                        foreach (var item in temp)
                        {
                            if (constellation == item.Name)
                            {
                                await bot.SendTextMessageAsync(Message.From.Id, item.Name);
                                await bot.SendTextMessageAsync(Message.From.Id, "На латыни - " + item.LatinName);
                                await bot.SendPhotoAsync(Message.From.Id, item.UrlMap);
                                await bot.SendPhotoAsync(Message.From.Id, item.UrlPhoto);
                                await bot.SendTextMessageAsync(Message.From.Id, item.Text);
                            }
                        }
                    }
                }
            }
        }


        [Obsolete]
        private static async void BotOnMessage(object sender, MessageEventArgs e)
        {
            Message = e.Message;
            string answerText = default;

            Console.WriteLine($"Сообщение от {Message.From.FirstName}, текст: {Message.Text}");

            firstName = Message.From.FirstName;
            //создается папка пользователя с подпапками
            if (!Directory.Exists(Path + $@"\{firstName}"))
            {
                Directory.CreateDirectory(Path + $@"\{firstName}");
                Directory.CreateDirectory(Path + $@"\{firstName}\Video");
                Directory.CreateDirectory(Path + $@"\{firstName}\Audio");
                Directory.CreateDirectory(Path + $@"\{firstName}\Photo");
                Directory.CreateDirectory(Path + $@"\{firstName}\Document");
            }
            //условие при котором формируются Inline кнопки созвездий согласно буквы
            if (Message.Type == MessageType.Text && Message.Text.Length == 1)
            {
                PrepareConstellations.ReturnInlineKeyboard(Message.Text);
                return;
            }

            switch (Message.Text)
            {
                case "/start":
                    answerText = @"Бот может общаться, может сохранять и показывать переданные ему файлы, " +
                                 "может рассказать Вам о созвездиях.\n" +
                                 "Используйте следующие команды:\n" +
                                 "/menu - выбор Меню\n";
                    await bot.SendTextMessageAsync(Message.From.Id, answerText);
                    break;
                case "/menu":
                    var keyboardButtons = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton("Инф. о созвездиях"),
                        new KeyboardButton("Сохраненные файлы")
                    }, resizeKeyboard: true);
                    await bot.SendTextMessageAsync(Message.Chat.Id, "Выбран пункт 'menu'", replyMarkup: keyboardButtons);
                    break;
                case "Сохраненные файлы":
                    var keyboardInline = new InlineKeyboardMarkup(new[]
                    {
                       new[]
                       {
                           InlineKeyboardButton.WithCallbackData("Аудио файлы", "Audio"),
                           InlineKeyboardButton.WithCallbackData("Видео файлы", "Video"),
                       },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Документы", "Document"),
                            InlineKeyboardButton.WithCallbackData("Фото", "Photo")
                        }
                    });
                    await bot.SendTextMessageAsync(Message.From.Id, "Укажите тип файлов на клавитатуре", replyMarkup: keyboardInline);
                    break;
                case "Инф. о созвездиях":
                    var alphabetKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new KeyboardButton("А"),
                            new KeyboardButton("Б"),
                            new KeyboardButton("В"),
                            new KeyboardButton("Г"),
                            new KeyboardButton("Д"),
                            new KeyboardButton("Е"),
                            new KeyboardButton("Ж"),
                            new KeyboardButton("З"),
                            new KeyboardButton("И")
                        },
                        new[]
                        {
                            new KeyboardButton("К"),
                            new KeyboardButton("Л"),
                            new KeyboardButton("М"),
                            new KeyboardButton("Н"),
                            new KeyboardButton("О"),
                            new KeyboardButton("П"),
                            new KeyboardButton("Р"),
                            new KeyboardButton("С"),
                            new KeyboardButton("Т")
                        },
                        new[]
                        {
                            new KeyboardButton("Ф"),
                            new KeyboardButton("Х"),
                            new KeyboardButton("Ц"),
                            new KeyboardButton("Ч"),
                            new KeyboardButton("Щ"),
                            new KeyboardButton("Э"),
                            new KeyboardButton("Ю"),
                            new KeyboardButton("Я"),
                            new KeyboardButton(":)")
                        },
                    }, resizeKeyboard: true);
                    await bot.SendTextMessageAsync(Message.Chat.Id, "Выбран пункт 'Инф. о созвездиях'", replyMarkup: alphabetKeyboard);
                    break;
                case ":)":
                    {
                        await bot.SendTextMessageAsync(Message.Chat.Id, "Рад, что Вам понравилось");
                        await bot.SendTextMessageAsync(Message.Chat.Id, "/menu");
                        break;
                    }
                default:
                    // Общение с ботом через DialogFlow
                    // Инициализируем аргументы ответа
                    if (Message.Type == MessageType.Text) // проверка если вдруг отправят вложение с пустым текстовым полем
                    {
                        SessionName session = SessionName.FromProjectSession(projectID, sessionID);
                        var queryInput = new QueryInput
                        {
                            Text = new TextInput
                            {
                                Text = Message.Text,
                                LanguageCode = "ru-ru"
                            }
                        };

                        // Создаем ответ пользователю
                        DetectIntentResponse response = await dFlowClient.DetectIntentAsync(session, queryInput);

                        answerText = response.QueryResult.FulfillmentText;

                        if (answerText == "")
                        {
                            //answerText = ">:P";
                            return;
                        }
                        await bot.SendTextMessageAsync(Message.Chat.Id, answerText); // отправляем пользователю ответ
                    }
                    break;
            }

            switch (Message.Type)
            {
                case MessageType.Photo:
                    Console.WriteLine("Получено фото ");
                    string photoFileId = Message.Photo[Message.Photo.Length - 1].FileId;
                    Download(photoFileId, Path + $@"\{firstName}\Photo\" + (Message.Photo[Message.Photo.Length - 1]).FileUniqueId + ".jpg");
                    await bot.SendTextMessageAsync(Message.From.Id, "я сохранил это");
                    break;
                case MessageType.Document:
                    Console.WriteLine("Получен документ " + Message.Document.FileName);
                    Download(e.Message.Document.FileId, Path + $@"\{firstName}\Document\" + Message.Document.FileName);
                    await bot.SendTextMessageAsync(Message.From.Id, "я сохранил это");
                    break;
                case MessageType.Video:
                    Console.WriteLine("Получен видео файл " + Message.Video.FileName);
                    Download(Message.Video.FileId, Path + $@"\{firstName}\Video\" + Message.Video.FileName);
                    await bot.SendTextMessageAsync(Message.From.Id, "я сохранил это");
                    break;
                case MessageType.Audio:
                    Console.WriteLine("Получен аудио файл " + Message.Audio.FileName);
                    Download(Message.Audio.FileId, Path + $@"\{firstName}\Audio\" + Message.Audio.FileName);
                    await bot.SendTextMessageAsync(Message.From.Id, "я сохранил это");
                    break;
            }

        }
    }
}
