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

namespace ConfidentialityDocSearcher.ViewModels.MainWindowVm
{
    internal class MainWindowViewModel : BaseViewModel
    {
        private readonly IAppConfig _appConfig;
        private readonly IUserDialogService _userDialogService;
        List<string> _confidentialFiles = new List<string>();
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
            _log.Debug("Команда поиска Word-документов");
            var searcher = new ConfidentialSearcher();
            var progress = new Progress<double>(p => ProgressValue = p);
            var status = new Progress<string>(s => StatusText = s);

            ((Command)BrowseCommand).Executable = false;
            ((Command)SearchCommand).Executable = false;
            ((Command)SaveCommand).Executable = false;

            _confidentialFiles = await searcher.SearchAsync(SearchPath, progress, status);

            ((Command)BrowseCommand).Executable = true;
            ((Command)SearchCommand).Executable = true;
            ((Command)SaveCommand).Executable = true;

            SearchResults.Clear();
            foreach (var file in _confidentialFiles)
            {
                SearchResults.Add(file);
            }
            OnPropertyChanged(nameof(SearchResults));
            _log.Debug("Поиск завершен");
        }

        private bool CanSearchCommandExecute(object p) => !string.IsNullOrEmpty(SearchPath);
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

        #region Window title

        private string _title;
        /// <summary>
        /// Заголовок окна
        /// </summary>
        public string Title { get => _title; set => Set(ref _title, value); }
        #endregion

        #region SearchPath

        private string _searchPath;
        public string SearchPath { get => _searchPath; set => Set(ref _searchPath, value); }
        #endregion

        #region StatusText

        private string _statusText;
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
        #endregion

        #region ProgressValue

        private double _progressValue;
        public double ProgressValue { get => _progressValue; set => Set(ref _progressValue, value); }
        #endregion


        /* ------------------------------------------------------------------------------------------------------------ */

    }
}