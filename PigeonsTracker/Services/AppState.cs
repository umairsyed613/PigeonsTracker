using System;

namespace PigeonsTracker.Services
{
    public class AppState
    {
        public event Action OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
        public string LanguageName { get; set; }

        public void ChangeLanguageName(string name)
        {
            LanguageName = name;
            NotifyStateChanged();
        }
    }
}
