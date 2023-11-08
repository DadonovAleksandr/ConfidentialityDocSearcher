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
    private List<string> _result = new List<string>();
    private double _deltaPercent = 0.1;
    private double _percent = 0.0;
    private double _lastPercent = 0.0;
    private int _fileCount = 0;
    public async Task<List<string>> SearchAsync(string dir, IProgress<double> progress = null, IProgress<string> status = null, 
        CancellationToken cancellation = default)
    {
        try
        {
            _log.Debug("Поиск docx-файлов");
            await Task.Run(() => SearchWordFiles(dir, progress, status, cancellation));
            _log.Debug("Поиск xlsx-файлов");
            await Task.Run(() => SearchExcelFiles(dir, progress, status, cancellation));
            _log.Debug("Поиск pdf-файлов");
            await Task.Run(() => SearchPdfFiles(dir, progress, status, cancellation));
            
            status?.Report("поиск завершен");
            return _result;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellation)
        {
            _log.Warn("Поиск отменен");
            status?.Report("поиск отменен");
            return _result;
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
            return new List<string>();
        }
    }

    public IEnumerable<string> Search(string dir)
    {
        string[] docFiles = Directory.GetFiles(dir, ".docx", SearchOption.AllDirectories);
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

    private void SearchWordFiles(string dir, IProgress<double> progress = null, IProgress<string> status = null,
        CancellationToken cancellation = default)
    {
        try
        {
            status?.Report("поиск docx-файлов");
            InitProgress();

            var files = Directory.EnumerateFiles(dir, "*.docx", SearchOption.AllDirectories);

            Task.Run(() => { DeltaPercentCalculate(files); });
            _fileCount = 0;
            foreach (var file in files)
            {
                if(cancellation.IsCancellationRequested)
                {
                    _log.Warn("Поиск docx-файлов отменен");
                    cancellation.ThrowIfCancellationRequested();
                }
                _log.Trace($"Анализ файла {file}");
                _fileCount++;
                _percent += _deltaPercent;
                ReportProgress(progress);
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

                using (var doc = WordprocessingDocument.Open(file, false))
                {
                    using (StreamReader reader = new StreamReader(doc.MainDocumentPart.GetStream()))
                    {
                        var documentText = reader.ReadToEnd();
                        if (documentText.Contains("confidentialityType"))
                        {
                            _log.Debug($"Файл {file} содержит конфиденциальную информацию");
                            _result.Add(file);
                        }
                        else
                        {
                            _log.Trace($"Файл {file} не содержит конфиденциальную информацию");
                        }
                    }
                }
            }
            CompleteProgress(progress);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellation) 
        {
            _log.Warn(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
        }
    }

    private void SearchExcelFiles(string dir, IProgress<double> progress = null, IProgress<string> status = null,
        CancellationToken cancellation = default)
    {
        try
        {
            status?.Report("поиск xlsx-файлов");
            var files = Directory.EnumerateFiles(dir, "*.xlsx", SearchOption.AllDirectories);
            InitProgress();

            Task.Run(() => { DeltaPercentCalculate(files); });
            foreach (var file in files)
            {
                if(cancellation.IsCancellationRequested)
                {
                    _log.Warn("Поиск xlsx-файлов отменен");
                    cancellation.ThrowIfCancellationRequested();
                }
                _log.Trace($"Анализ файла {file}");
                _fileCount++;
                _percent += _deltaPercent;
                ReportProgress(progress);
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
                var confconfidentialityFlag = false;
                using (var doc = SpreadsheetDocument.Open(file, false))
                {
                    foreach (var part in doc.GetAllParts())
                    {
                        using (StreamReader reader = new StreamReader(part.GetStream()))
                        {
                            var documentText = reader.ReadToEnd();
                            if (documentText.Contains("confidentialityType"))
                            {
                                _log.Debug($"Файл {file} содержит конфиденциальную информацию");
                                _result.Add(file);
                                confconfidentialityFlag = true;
                                break;
                            }
                        }
                    }

                    if (!confconfidentialityFlag)
                    {
                        _log.Trace($"Файл {file} не содержит конфиденциальную информацию");
                    }
                }
            }
            CompleteProgress(progress);
        }
        catch(OperationCanceledException ex) when (ex.CancellationToken == cancellation) 
        {
            _log.Warn(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
        }
    }

    private void SearchPdfFiles(string dir, IProgress<double> progress = null, IProgress<string> status = null,
        CancellationToken cancellation = default)
    {
        try
        {
            status?.Report("поиск pdf-файлов");
            var files = Directory.EnumerateFiles(dir, "*.pdf", SearchOption.AllDirectories);
            InitProgress();

            Task.Run(() => { DeltaPercentCalculate(files); });
            var searchedNames = _result.Select(r => Path.GetFileNameWithoutExtension(r));
            foreach (var file in files)
            {
                if(cancellation.IsCancellationRequested)
                {
                    _log.Warn("Поиск pdf-файлов отменен");
                    cancellation.ThrowIfCancellationRequested();
                }
                _log.Trace($"Анализ файла {file}");
                _fileCount++;
                _percent += _deltaPercent;
                ReportProgress(progress);
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

                var name = Path.GetFileNameWithoutExtension(file);
                if (searchedNames.Contains(name))
                {
                    _log.Debug($"Файл {file} содержит конфиденциальную информацию");
                    _result.Add(file);
                    continue;
                }
                else
                {
                    _log.Trace($"Файл {file} не содержит конфиденциальную информацию");
                }

            }
            CompleteProgress(progress);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellation) 
        {
            _log.Warn(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
        }
    }


    private void InitProgress()
    {
        _log.Trace($"Инициализация данных о прогрессе: процент = {_percent}, дельта = {_deltaPercent}, кол-во найденных файлов = {_fileCount}");
        _percent = 0.0;
        _deltaPercent = 0.1;
        _fileCount = 0;
    }

    private void CompleteProgress(IProgress<double> progress)
    {
        if(_percent > 99.0) return;
        
        _log.Warn($"Искуственное заврешение прогресса: процент = {_percent}, дельта = {_deltaPercent}, кол-во найденных файлов = {_fileCount}");
        _deltaPercent = (100.0 - _percent)/10.0;
        while (_percent < 100.0)
        {
            _percent += _deltaPercent;
            _log.Trace($"Автоинкремент прогресса: процент = {_percent}, дельта = {_deltaPercent}");
            Thread.Sleep(50);
            ReportProgress(progress);
        }
    }

    private void ReportProgress(IProgress<double> progress)
    {
        if(Math.Abs(_percent - _lastPercent) < 0.5) 
            return;
        _lastPercent = _percent;
        progress?.Report(_percent);
    }

    private void DeltaPercentCalculate(IEnumerable<string> collection)
    {
        Thread.CurrentThread.Priority = ThreadPriority.Highest;

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