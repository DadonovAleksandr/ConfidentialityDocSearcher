using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConfidentialityDocSearcher.Service;

internal class ConfidentialSearcher
{
    private static Logger _log = LogManager.GetCurrentClassLogger();
    private List<string> _files = new List<string>();
    private double _deltaPercent = 0.1;
    private double _percent = 0.0;
    private int _fileCount = 0;
    public async Task<List<string>> SearchAsync(string dir, IProgress<double> progress = null, IProgress<string> status = null)
    {
        var result = new List<string>();
        await Task.Run(() =>
        {
            status?.Report("поиск docx-файлов");
            var files = Directory.EnumerateFiles(dir, "*.docx", SearchOption.AllDirectories);
            
            Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                DeltaPercentCalculate(files);
            });
            _fileCount = 0;
            foreach (var file in files)
            {
                _log.Debug($"Анализ файла {file}");
                _fileCount++;
                _percent += _deltaPercent;
                progress?.Report(_percent);
                _log.Trace($"Прогресс {_percent}, дельта {_deltaPercent}");
                if (file is null)
                {
                    _log.Error($"Файл {file} не существует");
                    continue;
                }
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith("~"))
                {
                    _log.Warn($"Файл {file} является временным файлом и будет игнорирован");
                    continue;
                }
                try
                {
                    using (var doc = WordprocessingDocument.Open(file, false))
                    {
                        using (StreamReader reader = new StreamReader(doc.MainDocumentPart.GetStream()))
                        {
                            var documentText = reader.ReadToEnd();
                            if (documentText.Contains("confidentialityType"))
                            {
                                _log.Warn($"Файл {file} содержит конфиденциальную информацию");
                                result.Add(file);
                            }
                            else
                            {
                                _log.Debug($"Файл {file} не содержит конфиденциальную информацию");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message);
                    continue;
                }
            }
            while(_percent < 100.0)
            {
                _percent += _deltaPercent;
                Thread.Sleep(50);
                progress?.Report(_percent);
            }
            status?.Report("поиск завершен");
        });
        return result;
    }

    public IEnumerable<string> Search(string dir)
    {
        //var extensions = new List<string> { ".doc", ".docx" };
        string[] docFiles = Directory.GetFiles(dir, ".docx", SearchOption.AllDirectories);
                            //.Where(f => extensions.IndexOf(Path.GetExtension(f)) >= 0).ToArray();
        _log.Debug($"Найдено {docFiles.Length} файлов");
        _files.Clear();
        foreach (var file in docFiles)
        {
            _log.Debug($"Анализ файла {file}");

            if (file is null)
            {
                _log.Error($"Файл {file} не существует");
                continue;
            }
            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith("~"))
            {
                _log.Warn($"Файл {file} является временным файлом и будет игнорирован");
                continue;
            }

            var documentText = string.Empty;
            try
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    // Create Document
                    using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(file, false))
                    {
                        using (StreamReader reader = new StreamReader(wordDocument.MainDocumentPart.GetStream()))
                        {
                            documentText = reader.ReadToEnd();
                            if (documentText.Contains("confidentialityType"))
                            {
                                _log.Warn($"Файл {file} содержит конфиденциальную информацию");
                                _files.Add(file);
                            }
                            else
                            {
                                _log.Debug($"Файл {file} не содержит конфиденциальную информацию");
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                continue;
            }

        }
        return _files;
    }

    private void DeltaPercentCalculate(IEnumerable<string> collection)
    {
        var count = collection.Count();
        if(count <= _fileCount)
        {
            _log.Warn("Опоздали с расчетом дельты прогресса");
            return;
        }
        _deltaPercent = (100.0 - _percent) / (count - _fileCount);
        _log.Warn($"Дельта вычислена {_deltaPercent}, файлов {count}");
    }
}