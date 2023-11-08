namespace ConfidentialityDocSearcher.Model.AppSettings.AppConfig
{
    internal class AppConst
    {
        private static AppConst _instance;

        public static AppConst Get()
        {
            if (_instance is null)
                _instance = new AppConst();
            return _instance;
        }

        public string AppName => "ConfidentialityDocSearcher";
        public string AppDesciption => "Поиск конфиденциальных документов";
    }
}