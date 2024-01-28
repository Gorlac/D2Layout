using System.Drawing.Text;
using System.Runtime.InteropServices;

static class Program
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;

    [STAThread]
    static void Main()
    {
        var handle = GetConsoleWindow();

        // Cache la fenêtre de la console
        ShowWindow(handle, SW_HIDE);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new CounterForm());
    }
}

public class CounterForm : Form
{
    private int count = 0;
    private Label countLabel;
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_SHOWWINDOW = 0x0040;
    private List<Tuple<FlatButton, Label>> counters = new List<Tuple<FlatButton, Label>>();
    private TextBox newText;
    private Button addButton;
    private Font globalFont = new Font("Arial", 10); // Définition de la police globale
    private PrivateFontCollection privateFonts = new PrivateFontCollection();
    private bool isTextBoxVisible = false; // Pour suivre l'état du TextBox

    private Button moveButton; // Bouton pour déplacer la fenêtre
    private Point mouseDownPoint = Point.Empty; // Pour stocker la position de la souris lors du clic


    public CounterForm()
    {
        LoadCustomFont();
        LoadData(); // Charger les données avant d'initialiser les composants
        InitializeComponents();

        var handle = this.Handle;
        SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        ApplyGlobalFont(this.Controls); // Appliquer la police globale à tous les contrôles existants


        this.Size = new Size(500, 900);

        // Input
        newText = new TextBox { Location = new Point(10, 10), Size = new Size(200, 20), Visible = false, Name = "input" };
        addButton = new FlatButton { Text = "", Location = new Point(220, 10), Size = new Size(30, 30) };
        addButton.BackgroundImage = Image.FromFile("assets/plus.png");
        addButton.BackgroundImageLayout = ImageLayout.Stretch; // Ajuste l'image pour qu'elle remplisse le bouton.
        addButton.Click += AddButton_Click;

        this.Controls.Add(newText);
        this.Controls.Add(addButton);
    }

    private void InitializeComponents()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.TopMost = true;
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - 30,
                                  0);
        this.BackColor = Color.LimeGreen;
        this.TransparencyKey = Color.LimeGreen; // Rend le fond transparent

        this.Size = new Size(150, 60);

        // Bouton de déplacement
        moveButton = new FlatButton { Text = "", Width = 20, Height = 22, Name = "drag" };
        moveButton.BackgroundImage = Image.FromFile("assets/dragbtn.png");
        moveButton.MouseDown += MoveButton_MouseDown;
        moveButton.MouseMove += MoveButton_MouseMove;
        moveButton.MouseUp += MoveButton_Click;
        this.Controls.Add(moveButton);
    }

    private void LoadCustomFont()
    {
        // Chemin vers votre fichier de police dans le répertoire de sortie
        string fontPath = @"assets/exocet.TTF";

        // Charger la police
        privateFonts.AddFontFile(fontPath);

        // Créer une instance de la police
        globalFont = new Font(privateFonts.Families[0], 12); // Ajustez la taille selon vos besoins

        ApplyGlobalFont(this.Controls); // Appliquer la police globale à tous les contrôles existants
    }

    private void UpdateCount()
    {
        countLabel.Text = count.ToString();
    }

    public class FlatButton : Button
    {
        public FlatButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            TextAlign = ContentAlignment.MiddleCenter;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
            //pevent.Graphics.DrawImage(Image.FromFile("assets/plus.png"), pevent.ClipRectangle);
        }
    }
    private void AddCounter(string text, int initialValue)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var button = new FlatButton
        {
            Text = text,
            Size = new Size(30, 30),
            BackgroundImage = Image.FromFile("assets/plus.png"),
            BackgroundImageLayout = ImageLayout.Stretch
        };

        var label = new Label
        {
            Text = initialValue.ToString(),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            Size = new Size(90, 20)
        };

        var textLabel = new Label
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Orange,
            Size = new Size(150, 20)
        };

        var buttonDelete = new FlatButton
        {
            Text = text,
            Size = new Size(20, 20),
            BackgroundImage = Image.FromFile("assets/resetbtn.png"),
            BackgroundImageLayout = ImageLayout.Stretch
        };

        int offsetY = counters.Count * 30 + 40; // Décalage vertical pour chaque nouveau compteur

        textLabel.Location = new Point(10, offsetY);
        button.Location = new Point(160, offsetY);
        label.Location = new Point(190, offsetY);
        buttonDelete.Location = new Point(300, offsetY);

        button.Click += (s, e) =>
        {
            label.Text = (int.Parse(label.Text) + 1).ToString();
            SaveData(); // Sauvegarder les données après chaque incrément
        };
        buttonDelete.Click += (s, e) =>
        {
            label.Dispose();
            button.Dispose();
            textLabel.Dispose();
            buttonDelete.Dispose();
            SaveData(); // Sauvegarder les données après chaque incrément
        };

        this.Controls.Add(button);
        this.Controls.Add(label);
        this.Controls.Add(textLabel);
        this.Controls.Add(buttonDelete);

        counters.Add(new Tuple<FlatButton, Label>(button, label));
    }

    private void AddButton_Click(object sender, EventArgs e)
    {
        if (!isTextBoxVisible)
        {
            // Premier clic : rendre le TextBox visible pour la saisie de texte
            newText.Visible = true;
            newText.Focus(); // Mettre le focus sur le TextBox pour saisie immédiate
            isTextBoxVisible = true;
        }
        else
        {
            // Deuxième clic : ajouter l'incrément avec le texte saisi et cacher le TextBox
            if (!string.IsNullOrWhiteSpace(newText.Text)) // Vérifier que le texte n'est pas vide
            {
                AddCounter(newText.Text, 0); // Utiliser la méthode AddCounter pour ajouter un compteur
                newText.Text = ""; // Réinitialiser le TextBox
            }
            newText.Visible = false; // Cacher le TextBox
            isTextBoxVisible = false;
        }

        ApplyGlobalFont(this.Controls); // Appliquer la police globale à tous les contrôles existants

        SaveData();
    }

    private void ApplyGlobalFont(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            control.Font = globalFont;
            if (control.HasChildren)
            {
                ApplyGlobalFont(control.Controls); // Appliquer récursivement la police aux contrôles enfants
            }
        }
    }

    private void SaveData()
    {
        using (StreamWriter writer = new StreamWriter("data.csv"))
        {
            foreach (var counter in counters)
            {
                var buttonText = counter.Item1.Text; // Texte du bouton
                var countValue = counter.Item2.Text; // Valeur du compteur
                writer.WriteLine($"{buttonText},{countValue}");
            }
        }
    }

    private void LoadData()
    {
        if (File.Exists("data.csv"))
        {
            using (StreamReader reader = new StreamReader("data.csv"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int initialValue))
                    {
                        AddCounter(parts[0], initialValue); // Utiliser AddCounter pour ajouter un compteur avec le texte et la valeur chargés
                    }
                }
            }
        }
    }

    private void MoveButton_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            mouseDownPoint = new Point(e.X, e.Y);
        }
    }

    private void MoveButton_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            this.Left += e.X - mouseDownPoint.X;
            this.Top += e.Y - mouseDownPoint.Y;
        }
    }
    private void MoveButton_Click(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right) // Vérifie si le bouton droit de la souris a été cliqué
        {
            foreach (Control control in this.Controls)
            {
                if (control.Name != "input" && control.Name != "drag")
                {
                    control.Visible = !control.Visible;
                }
            }
        }
    }

}