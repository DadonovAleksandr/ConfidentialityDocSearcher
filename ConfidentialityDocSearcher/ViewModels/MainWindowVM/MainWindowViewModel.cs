using ConfidentialityDocSearcher.Infrastructure.Commands;
using ConfidentialityDocSearcher.Model.AppSettings.AppConfig;
using ConfidentialityDocSearcher.Service.UserDialogService;
using ConfidentialityDocSearcher.ViewModels.Base;
using Ookii.Dialogs.Wpf;
using ProjectVersionInfo;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System;
using ConfidentialityDocSearcher.Service;
using ConfidentialityDocSearcher.Infrastructure.Commands.Base;
using ConfidentialityDocSearcher.Infrastructure.Helpers;
using System.Threading;

namespace ConfidentialityDocSearcher.ViewModels.MainWindowVm
{
    internal class MainWindowViewModel : BaseViewModel
    {
        private readonly IAppConfig _appConfig;
        private readonly IUserDialogService _userDialogService;
        List<string> _confidentialFiles = new List<string>();
        private CancellationTokenSource _processCancellation;
        /* ------------------------------------------------------------------------------------------------------------ */
        public MainWindowViewModel(IUserDialogService userDialogService)
        {
            _log.Debug($"Вызов конструктора {GetType().Name}");
            _appConfig = AppConfig.GetConfigFromDefaultPath();
            _userDialogService = userDialogService;

            var prjVersion = new ProjectVersion(Assembly.GetExecutingAssembly());
            Title = $"{AppConst.Get().AppDesciption} {prjVersion.Version}";

            SearchResults = new ObservableCollection<string>();

            #region Commands
            BrowseCommand = new RelayCommand(OnBrowseCommandExecuted, CanBrowseCommandExecute);
            SearchCommand = new RelayCommand(OnSearchCommandExecuted, CanSearchCommandExecute);
            CancelCommand = new RelayCommand(OnCancelCommandExecuted, CanCancelCommandExecute);
            SaveCommand = new RelayCommand(OnSaveCommandExecuted, CanSaveCommandExecute);
            Exit = new RelayCommand(OnExitExecuted, CanExitExecute);
            #endregion

        }

        /// <summary>
        /// Действия выполняемые при закрытии основной формы
        /// </summary>
        public void OnExit()
        {
            //_projectConfigurationRepository?.Save();
        }
        /* ------------------------------------------------------------------------------------------------------------ */
        
        #region Commands

        #region BrowseCommand
        public ICommand BrowseCommand { get; }
        private void OnBrowseCommandExecuted(object p)
        {
            _log.Debug("Команда добавить директорию поиска");
            VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog()
            {
                Description = "Выберите директорию поиска",
                UseDescriptionForTitle = true,
                Multiselect = false
            };
            var dialogResult = ofd.ShowDialog();
            if (!dialogResult ?? false)
            {
                _log.Debug("Диалог выбора директории завершился отменой");
                return;
            }
            var selectedFolder = ofd.SelectedPath;
            if (!Directory.Exists(selectedFolder))
            {
                _log.Error($"Директория {selectedFolder} несуществует");
                return;
            }

            SearchPath = selectedFolder;
            _log.Debug($"Выбрана директория поиска: {selectedFolder}");
        }

        private bool CanBrowseCommandExecute(object p) => true;
        #endregion

        #region SearchCommand
        public ICommand SearchCommand { get; }
        private async void OnSearchCommandExecuted(object p)
        {
            _log.Debug("Команда поиска конфиденциальных документов");
            var searcher = new ConfidentialSearcher();
            var progress = new Progress<double>(p => ProgressValue = p);
            var status = new Progress<string>(s => StatusText = s);

            _processCancellation = new CancellationTokenSource();
            var cancellation = _processCancellation.Token;


            ((Command)BrowseCommand).Executable = false;
            ((Command)SearchCommand).Executable = false;
            ((Command)SaveCommand).Executable = false;

            try
            {
                _confidentialFiles = await searcher.SearchAsync(SearchPath, progress, status, cancellation);

            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellation) { }
            finally
            {
                _processCancellation.Dispose();
                _processCancellation = null;
            }
            
            ((Command)BrowseCommand).Executable = true;
            ((Command)SearchCommand).Executable = true;
            ((Command)SaveCommand).Executable = true;

            SearchResults.AddClear(_confidentialFiles);
            _log.Debug("Поиск завершен");
        }

        private bool CanSearchCommandExecute(object p) => !string.IsNullOrEmpty(SearchPath);
        #endregion

        #region CancelCommand
        public ICommand CancelCommand { get; }
        private async void OnCancelCommandExecuted(object p)
        {
            _log.Debug("Команда отмены поиска конфиденциальных документов");
            if(_processCancellation is null)
            {
                _log.Debug("Команда отмены поиска конфиденциальных документов не может быть выполнена, т.к. поиск не запущен");
                return;
            }
            _processCancellation.Cancel();
        }

        private bool CanCancelCommandExecute(object p) => _processCancellation != null && !_processCancellation.IsCancellationRequested;
        #endregion

        #region SaveCommand
        public ICommand SaveCommand { get; }
        private void OnSaveCommandExecuted(object p)
        {
            _log.Debug("Команда сохранения результатов");
            var ofd = new VistaSaveFileDialog()
            {
                Title = "Сохранить результаты поиска",
                Filter = "Текстовый файл (*.txt)|*.txt",
                DefaultExt = "txt",
                AddExtension = true,
                FileName = "Результаты поиска.txt"
            };
            var dialogResult = ofd.ShowDialog();
            if (!dialogResult ?? false)
            {
                _log.Debug("Диалог сохранения завершился отменой");
                return;
            }
            var selectedFile = ofd.FileName;
            File.WriteAllLines(selectedFile, _confidentialFiles);
            _log.Debug($"Результаты поиска сохранены в файл {selectedFile}");
        }

        private bool CanSaveCommandExecute(object p) => _confidentialFiles.Count > 0;
        #endregion

        #region Exit
        public ICommand Exit { get; }
        private void OnExitExecuted(object p) => Application.Current.Shutdown();
        private bool CanExitExecute(object p) => true;
        #endregion

        #endregion

        /* ------------------------------------------------------------------------------------------------------------ */
        
        public ObservableCollection<string> SearchResults { get; set; }

        /// <summary>
        /// Заголовок окна
        /// </summary>
        public string Title { get => Get<string>(); set => Set<string>(value); }
        
        /// <summary>
        /// Директроия поиска
        /// </summary>
        public string SearchPath { get => Get<string>(); set => Set<string>(value); }

        /// <summary>
        /// Статус выполнения
        /// </summary>
        public string StatusText { get => Get<string>(); set => Set<string>(value); }

        /// <summary>
        /// Прогресс выполнения
        /// </summary>
        public double ProgressValue { get => Get<double>(); set => Set<double>(value); }

        /* ------------------------------------------------------------------------------------------------------------ */

    }
}