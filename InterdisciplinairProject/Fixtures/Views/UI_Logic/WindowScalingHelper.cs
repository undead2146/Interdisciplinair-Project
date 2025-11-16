using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// De namespace moet overeenkomen met de locatie waar je het bestand plaatst.
// Bijvoorbeeld: InterdisciplinairProject.Fixtures.Views.UI_Logic
namespace InterdisciplinairProject.Fixtures.Views.UI_Logic
{
    /// <summary>
    /// Biedt attached properties om schaal- en scrollgedrag van UI-elementen te forceren.
    /// Dit helpt om ervoor te zorgen dat essentiële elementen (knoppen) zichtbaar blijven, 
    /// ongeacht de afmetingen van het hoofdvenster (Requirement: knoppen moeten overal zichtbaar zijn).
    /// </summary>
    public static class WindowScalingHelper
    {
        // ----------------------------------------------------
        // I. Attached Property Registratie
        // ----------------------------------------------------

        public static readonly DependencyProperty EnsureButtonsVisibleProperty =
            DependencyProperty.RegisterAttached(
                "EnsureButtonsVisible",
                typeof(bool),
                typeof(WindowScalingHelper),
                new PropertyMetadata(false, OnEnsureButtonsVisibleChanged));

        // Getters en Setters voor de Attached Property
        public static bool GetEnsureButtonsVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnsureButtonsVisibleProperty);
        }

        public static void SetEnsureButtonsVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(EnsureButtonsVisibleProperty, value);
        }

        // ----------------------------------------------------
        // II. Callback voor de UI-Logica
        // ----------------------------------------------------

        private static void OnEnsureButtonsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Panel panel && (bool)e.NewValue)
            {
                // De logica: Garandeer dat dit paneel scrollbaar is, indien nodig.
                // Dit is de puurste vorm van de fix buiten de ViewModel en de Code-Behind.

                // Zoek de dichtstbijzijnde ScrollViewer parent.
                ScrollViewer scrollViewer = FindVisualParent<ScrollViewer>(panel);

                if (scrollViewer == null)
                {
                    // Indien er GEEN ScrollViewer is, is de enige garantie dat knoppen zichtbaar blijven
                    // dat dit paneel de verticale scrollbars op de hoofd-ContentControl/Window aanzet.
                    // Omdat we de Window niet direct kunnen modificeren, is de beste actie 
                    // de developer te verplichten het element in een ScrollViewer te plaatsen.

                    // Maar, om toch een actie te ondernemen:

                    // We forceren de hoogte en breedte van de knoppenstack om niet oneindig door te lopen.
                    // Dit voorkomt dat het paneel de hele beschikbare ruimte opeist.
                    panel.HorizontalAlignment = HorizontalAlignment.Left;
                    panel.VerticalAlignment = VerticalAlignment.Top;
                }
                else
                {
                    // Als er een ScrollViewer is, zorg dan dat deze actief is voor verticaal scrollen.
                    scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                }
            }
        }

        // ----------------------------------------------------
        // III. Helper Methode
        // ----------------------------------------------------

        // Functie om de visuele boom omhoog te doorlopen en de parent te vinden.
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                {
                    return typedParent;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}