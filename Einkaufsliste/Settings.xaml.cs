namespace Einkaufsliste;

public partial class Settings : ContentPage
{
    public Settings()
    {
        InitializeComponent();
    }

    public void Btn_Liste_Loeschen_Clicked(object sender, EventArgs e)
    {
        GroceryStorage.DeleteFile();
    }

}