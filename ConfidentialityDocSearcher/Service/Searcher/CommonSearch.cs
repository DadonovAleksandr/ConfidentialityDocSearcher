using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ConfidentialityDocSearcher.Service.Searcher;

internal partial class ConfidentialSearcher
{
    internal string[] SearchDirectoriesCustom(string rootDir, IProgress<double> progress = null,
        CancellationToken cancellation = default)
    {
        var dirs = new List<string>();
        dirs.Add(rootDir);
        dirs.AddRange(Directory.GetDirectories(rootDir, "*", SearchOption.AllDirectories));
        return dirs.ToArray();
    }

    internal string[] SearchDirectoriesRecursive(string rootDir, IProgress<string> status = null,
        CancellationToken cancellation = default)
    {
        var dirs = new List<string>();
        dirs.Add(rootDir);
        try
        {
            SubDirSearch(rootDir, dirs, status, cancellation);
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
        return dirs.ToArray();
    }

    internal string[] SearchDirectories(string rootDir, IProgress<string> status = null,
        CancellationToken cancellation = default)
    {
        var tempDirs = new List<string>() { rootDir };
        var result = new List<string>() { rootDir };
        var lastCount = 0;

        while (tempDirs.Count > 0)
        {
            try
            {
                if (cancellation.IsCancellationRequested)
                {
                    _log.Warn("Операция отменена");
                    cancellation.ThrowIfCancellationRequested();
                }
                var dir = tempDirs[tempDirs.Count - 1];
                tempDirs.RemoveAt(tempDirs.Count - 1);
                result.Add(dir);
                if(result.Count - lastCount > 100)
                {
                    lastCount = result.Count;
                    status?.Report($"формирование списка директорий поиска: {result.Count}");
                }
                if (!Directory.Exists(dir))
                {
                    _log.Error($"Директория не существует: {dir}");
                    continue;
                }
                tempDirs.AddRange(Directory.GetDirectories(dir));
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
        status?.Report($"формирование списка директорий поиска: {result.Count}");
        return result.ToArray();
    }

    private void SubDirSearch(string dir, List<string> dirs, IProgress<string> status = null,
        CancellationToken cancellation = default)
    {
        foreach(var d in Directory.GetDirectories(dir)) 
        {
            if (cancellation.IsCancellationRequested)
            {
                _log.Warn("Операция отменена");
                cancellation.ThrowIfCancellationRequested();
            }
            dirs.Add(d);
            status?.Report($"формирование списка директорий поиска: {dir.Length}");
            SubDirSearch(d, dirs);
        }
    }

    internal string[] SearchFiles(string[] dirs, string searchPattern, IProgress<double> progress = null,
        CancellationToken cancellation = default)
    {
        var percent = 0.0;
        var deltaPercent = DeltaPercentCalculate(dirs.Length);
        var files = new List<string>();
        foreach (var d in dirs)
        {
            if (cancellation.IsCancellationRequested)
            {
                _log.Warn("Операция отменена");
                cancellation.ThrowIfCancellationRequested();
            }
            if(!Directory.Exists(d))
            {
                _log.Error($"Директория не существует: {d}");
                continue;
            }
            files.AddRange(Directory.GetFiles(d, searchPattern, SearchOption.TopDirectoryOnly));
            percent = ReportProgress(progress, percent, deltaPercent);
        }
        return files.ToArray();
    }

    private double ReportProgress(IProgress<double> progress, double percent, double delta)
    {
        if (delta < 0)
        {
            _log.Error($"Дельта прогресса не может быть {delta}");
            return percent;
        }

        percent += delta;
        
        if (Math.Abs(percent - _lastPercent) < 0.5)
            return percent;
        
        _lastPercent = percent;
        progress?.Report(percent/100.0);
        return percent;
    }

    private double DeltaPercentCalculate(int length)
    {
        var deltaPercent = 100.0 / length;
        _log.Debug($"Расчет дельты прогресса: дельта = {deltaPercent}, кол-во сущностей = {length}");
        return deltaPercent;
    }

    private void CompleteProgress(IProgress<double> progress, double percent)
    {
        if (percent > 99.0) return;

        _log.Warn($"Искуственное заврешение прогресса: процент = {percent}");
        var deltaPercent = (100.0 - percent) / 10.0;
        while (percent < 100.0)
        {
            percent = ReportProgress(progress, percent, deltaPercent);
            _log.Trace($"Автоинкремент прогресса: процент = {percent}, дельта = {deltaPercent}");
            Thread.Sleep(50);
        }
    }
}