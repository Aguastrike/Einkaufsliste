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
            // Reset visual checked state when the template is reused for a new item.
            // Temporarily detach the CheckedChanged handler so resetting IsChecked
            // does not trigger the deletion logic for a recycled template.
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
            // Switch the app to dark mode
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
            // Switch the app to light mode
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

            // If unchecked, cancel any pending deletion for this item
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

            // When checked, schedule deletion after 5 seconds but allow cancellation
            var tokenSource = new CancellationTokenSource();
            lock (_pendingLock)
            {
                // If there's already a pending deletion, cancel it first
                if (_pendingDeletions.TryGetValue(item, out var existing))
                {
                    existing.Cancel();
                    existing.Dispose();
                    _pendingDeletions.Remove(item);
                }

                _pendingDeletions[item] = tokenSource;
            }

            // Fire-and-forget task to wait and then remove the item on the UI thread
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), tokenSource.Token);

                    // If not cancelled, remove the item on the UI thread and save
                    await Dispatcher.DispatchAsync(async () =>
                    {
                        GroceryStorage.Items.Remove(item);
                        await GroceryStorage.SaveAsync();
                    });
                }
                catch (OperationCanceledException)
                {
                    // cancellation requested - do nothing
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
