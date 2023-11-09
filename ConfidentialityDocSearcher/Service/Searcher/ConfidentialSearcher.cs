using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConfidentialityDocSearcher.Service.Searcher;

internal partial class ConfidentialSearcher
{
    private static Logger _log = LogManager.GetCurrentClassLogger();
    private double _lastPercent = 0.0;
    public async Task<List<string>> SearchAsync(string dir, IProgress<double> progress = null, IProgress<string> status = null, 
        CancellationToken cancellation = default)
    {
        var result = new List<string>();
        try
        {
            await Task.Run(() => 
            {
                
                _log.Debug("Этап 1: формирование директорий для поиска");
                status?.Report("формирование списка директорий поиска");
                progress?.Report(0.0);
                //var dirs = SearchDirectoriesCustom(dir, progress, cancellation);
                //_log.Debug($"Кол-во директорий поиска: {dirs.Length}");
                var dirs = SearchDirectories(dir, status, cancellation);
                _log.Debug($"Кол-во директорий поиска: {dirs.Length}");

                _log.Debug("Этап 2: формирование списка docx-файлов");
                status?.Report("поиск docx-файлов: формирование списка файлов");
                var docxFiles = SearchFiles(dirs, "*.docx", progress, cancellation);
                _log.Debug($"Кол-во docx-файлов: {docxFiles.Length}");

                _log.Debug("Этап 3: анализ docx-файлов");
                status?.Report("поиск docx-файлов: анализ файлов");
                result.AddRange(AnalysisWordFiles(docxFiles, progress, cancellation));

                _log.Debug("Этап 4: формирование списка xlsx-файлов");
                status?.Report("поиск xlsx-файлов: формирование списка файлов");
                var xlsxFiles = SearchFiles(dirs, "*.xlsx", progress, cancellation);
                _log.Debug($"Кол-во xlsx-файлов: {xlsxFiles.Length}");

                _log.Debug("Этап 5: анализ xlsx-файлов");
                status?.Report("поиск xlsx-файлов: анализ файлов");
                result.AddRange(AnalysisExcelFiles(xlsxFiles, progress, cancellation));

                _log.Debug("Этап 6: формирование списка pdf-файлов");
                status?.Report("поиск pdf-файлов: формирование списка файлов");
                var pdfFiles = SearchFiles(dirs, "*.pdf", progress, cancellation);
                _log.Debug($"Кол-во pdf-файлов: {pdfFiles.Length}");

                _log.Debug("Этап 7: анализ pdf-файлов");
                status?.Report("поиск pdf-файлов: анализ файлов");
                result.AddRange(AnalysisPdfFiles(pdfFiles, result.ToArray(), progress, cancellation));

            });

            status?.Report("поиск завершен");
            progress?.Report(1.0);
            return result;

        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellation)
        {
            _log.Warn("Поиск отменен");
            status?.Report("поиск отменен");
            return result;
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
            return new List<string>();
        }
    }
}