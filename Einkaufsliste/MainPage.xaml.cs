namespace Einkaufsliste
{
    public partial class MainPage : ContentPage
    {
        private readonly object _pendingLock = new();
        private readonly Dictionary<string, CancellationTokenSource> _pendingDeletions = new();

        public MainPage()
        {
            InitializeComponent();
            Btn_light_mode.IsEnabled = false;
            Btn_light_mode.IsVisible = false;

            Btn_dark_mode_settings.IsEnabled = false;
            Btn_dark_mode_settings.IsVisible = false;

            clv_groceries.ItemsSource = GroceryStorage.Items;
            GroceryStorage.LoadAsync();

        }

        private void CheckBox_BindingContextChanged(object sender, EventArgs e)
        {
            // Visuellen Checked-Zustand zurücksetzen, wenn die Vorlage für ein neues Element wiederverwendet wird.
            // Vorübergehend den CheckedChanged-Handler abmelden, damit das Zurücksetzen von IsChecked
            // die Löschlogik für eine wiederverwendete Vorlage nicht auslöst.
            if (sender is CheckBox cb)
            {
                cb.CheckedChanged -= CheckBox_CheckedChanged;
                cb.IsChecked = false;
                cb.CheckedChanged += CheckBox_CheckedChanged;
            }
        }

        public async void Btn_Add_Clicked(object sender, EventArgs e)
        {
            string text = await DisplayPromptAsync(
                "Gegenstand Hinzufügen",
                "Bitte etwas eingeben:"
            );

            if (!string.IsNullOrWhiteSpace(text))
            {
                GroceryStorage.Items.Add(text);
                await GroceryStorage.SaveAsync();
            }

        }

        public async void Btn_Settings_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new Settings());
        }

        public void Btn_Dark_Mode_Clicked(object sender, EventArgs e)
        {
            // App auf Dunkelmodus umschalten
            Application.Current.UserAppTheme = AppTheme.Dark;
            Btn_dark_mode.IsVisible = false;
            Btn_dark_mode.IsEnabled = false;
            Btn_light_mode.IsVisible = true;
            Btn_light_mode.IsEnabled = true;


            Btn_ligth_mode_settings.IsVisible = false;
            Btn_ligth_mode_settings.IsEnabled = false;
            Btn_dark_mode_settings.IsVisible = true;
            Btn_dark_mode_settings.IsEnabled = true;
        }


        public void Btn_Add_Clicked_Mode_Clicked(object sender, EventArgs e)
        {
            // App auf Hellmodus umschalten
            Application.Current.UserAppTheme = AppTheme.Light;
            Btn_dark_mode.IsVisible = true;
            Btn_dark_mode.IsEnabled = true;
            Btn_light_mode.IsVisible = false;
            Btn_light_mode.IsEnabled = false;

            Btn_ligth_mode_settings.IsVisible = true;
            Btn_ligth_mode_settings.IsEnabled = true;
            Btn_dark_mode_settings.IsVisible = false;
            Btn_dark_mode_settings.IsEnabled = false;
        }

        private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox cb || cb.BindingContext is not string item)
                return;

            // Wenn deaktiviert, eine ausstehende Löschung dieses Elements abbrechen
            if (!e.Value)
            {
                lock (_pendingLock)
                {
                    if (_pendingDeletions.TryGetValue(item, out var cts))
                    {
                        cts.Cancel();
                        cts.Dispose();
                        _pendingDeletions.Remove(item);
                    }
                }

                return;
            }

            // Wenn aktiviert, Löschung nach 5 Sekunden planen, aber Stornierung ermöglichen
            var tokenSource = new CancellationTokenSource();
            lock (_pendingLock)
            {
                // Falls bereits eine Löschung aussteht, diese zuerst abbrechen
                if (_pendingDeletions.TryGetValue(item, out var existing))
                {
                    existing.Cancel();
                    existing.Dispose();
                    _pendingDeletions.Remove(item);
                }

                _pendingDeletions[item] = tokenSource;
            }

            // Hintergrundtask: warten und dann das Element im UI-Thread entfernen
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), tokenSource.Token);

                    // Wenn nicht abgebrochen, Element im UI-Thread entfernen und speichern
                    await Dispatcher.DispatchAsync(async () =>
                    {
                        GroceryStorage.Items.Remove(item);
                        await GroceryStorage.SaveAsync();
                    });
                }
                catch (OperationCanceledException)
                {
                    // Abbruch angefordert - nichts tun
                }
                finally
                {
                    lock (_pendingLock)
                    {
                        if (_pendingDeletions.TryGetValue(item, out var cts))
                        {
                            cts.Dispose();
                            _pendingDeletions.Remove(item);
                        }
                    }
                }
            }, tokenSource.Token);
        }
    }
}
