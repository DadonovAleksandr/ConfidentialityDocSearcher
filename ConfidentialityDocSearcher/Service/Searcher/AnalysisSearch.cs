using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ConfidentialityDocSearcher.Service.Searcher;

internal partial class ConfidentialSearcher
{
    
    private string[] AnalysisWordFiles(string[] files, IProgress<double> progress = null, CancellationToken cancellation = default)
    {
        if(files is null) 
            throw new ArgumentNullException(nameof(files));
        if(!files.Any())
            return Enumerable.Empty<string>().ToArray();

        var result = new List<string>();
        var percent = 0.0;
        var deltaPercent = DeltaPercentCalculate(files.Length);

        
        foreach (var file in files)
        {
            try
            {
                if (cancellation.IsCancellationRequested)
                {
                    _log.Warn("Поиск docx-файлов отменен");
                    cancellation.ThrowIfCancellationRequested();
                }
                _log.Trace($"Анализ файла {file}");

                if (file is null || !File.Exists(file))
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
                            result.Add(file);
                        }
                        else
                        {
                            _log.Trace($"Файл {file} не содержит конфиденциальную информацию");
                        }
                    }
                }
                percent = ReportProgress(progress, percent, deltaPercent);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellation)
            {
                _log.Warn(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                continue;
            }    
        }
        CompleteProgress(progress, percent);
        return result.ToArray();
    }

    private string[] AnalysisExcelFiles(string[] files, IProgress<double> progress = null, CancellationToken cancellation = default)
    {
        if (files is null)
            throw new ArgumentNullException(nameof(files));
        if (!files.Any())
            return Enumerable.Empty<string>().ToArray();

        var result = new List<string>();
        var percent = 0.0;
        var deltaPercent = DeltaPercentCalculate(files.Length);

        foreach (var file in files)
        {
            try
            {
                if (cancellation.IsCancellationRequested)
                {
                    _log.Warn("Поиск xlsx-файлов отменен");
                    cancellation.ThrowIfCancellationRequested();
                }
                _log.Trace($"Анализ файла {file}");
                
                if (file is null || !File.Exists(file))
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
                                result.Add(file);
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
                percent = ReportProgress(progress, percent, deltaPercent);
                _log.Trace($"Прогресс {percent}, дельта {deltaPercent}");

            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellation)
            {
                _log.Warn(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                continue;
            }
        }
        CompleteProgress(progress, percent);
        return result.ToArray();
    }

    private string[] AnalysisPdfFiles(string[] files, string[] templates, IProgress<double> progress = null, CancellationToken cancellation = default)
    {
        if (files is null)
            throw new ArgumentNullException(nameof(files));
        if (!files.Any())
            return Enumerable.Empty<string>().ToArray();

        var result = new List<string>();
        var percent = 0.0;
        var deltaPercent = DeltaPercentCalculate(files.Length);

        var searchedNames = templates.Select(r => Path.GetFileNameWithoutExtension(r));
        foreach (var file in files)
        {
            try
            {
                if (cancellation.IsCancellationRequested)
                {
                    _log.Warn("Поиск pdf-файлов отменен");
                    cancellation.ThrowIfCancellationRequested();
                }
                _log.Trace($"Анализ файла {file}");
                if (file is null || !File.Exists(file))
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
                    result.Add(file);
                    continue;
                }
                else
                {
                    _log.Trace($"Файл {file} не содержит конфиденциальную информацию");
                }
                percent = ReportProgress(progress, percent, deltaPercent);
                _log.Trace($"Прогресс {percent}, дельта {deltaPercent}");


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
        CompleteProgress(progress, percent);
        return result.ToArray();
    }



    

    
}