using System.Linq;
using System.Numerics;
using Content.Client.ADT.Bark;
using Content.Client.ADT.SpeechBarks;
using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.SpeechBarks;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<BarkPrototype> _barkList = new();
    private FancyWindow? _barkWindow;

    private void InitializeBarks()
    {
        _barkList = _prototypeManager
            .EnumeratePrototypes<BarkPrototype>()
            .Where(o => o.RoundStart)
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();

        BarkProtoButton.OnPressed += _ => OpenBarkWindow();
        BarkPlayButton.OnPressed += _ => PlayPreviewBark();
    }

    private void OpenBarkWindow()
    {
        if (Profile is null)
            return;

        if (_barkWindow != null)
        {
            _barkWindow.Close();
            _barkWindow = null;
        }

        var barkTab = new BarkTab();
        barkTab.SetSelectedBark(
            Profile.Bark.Proto,
            Profile.Bark.Pitch,
            Profile.Bark.MinVar,
            Profile.Bark.MaxVar);

        barkTab.OnBarkSelected += OnBarkSelected;
        barkTab.OnPitchChanged += OnBarkPitchChanged;
        barkTab.OnMinVarChanged += OnBarkMinVarChanged;
        barkTab.OnMaxVarChanged += OnBarkMaxVarChanged;

        _barkWindow = new FancyWindow
        {
            Title = Loc.GetString("humanoid-profile-editor-bark-window-title"),
            MinSize = new Vector2(750, 600),
        };
        _barkWindow.ContentsContainer.AddChild(barkTab);
        _barkWindow.OnClose += () =>
        {
            _barkWindow = null;
        };
        _barkWindow.OpenCentered();
    }

    private void OnBarkSelected(string barkId)
    {
        SetBarkProto(barkId);
        UpdateBarkButtonText();
        UpdateSaveButton();
    }

    private void OnBarkPitchChanged(float pitch)
    {
        SetBarkPitch(pitch);
        UpdateSaveButton();
    }

    private void OnBarkMinVarChanged(float minVar)
    {
        SetBarkMinVariation(minVar);
        UpdateSaveButton();
    }

    private void OnBarkMaxVarChanged(float maxVar)
    {
        SetBarkMaxVariation(maxVar);
        UpdateSaveButton();
    }

    private void UpdateBarkVoicesControls()
    {
        if (Profile is null)
            return;

        UpdateBarkButtonText();
        // Обновляем окно барков если оно открыто
        if (_barkWindow != null && _barkWindow.ContentsContainer.ChildCount > 0)
        {
            if (_barkWindow.ContentsContainer.GetChild(0) is BarkTab barkTab)
            {
                barkTab.SetSelectedBark(
                    Profile.Bark.Proto,
                    Profile.Bark.Pitch,
                    Profile.Bark.MinVar,
                    Profile.Bark.MaxVar);
            }
        }
    }

    private void UpdateBarkButtonText()
    {
        if (Profile is null)
            return;

        var bark = _barkList.FirstOrDefault(b => b.ID == Profile.Bark.Proto);
        BarkProtoButton.Text = bark != null
            ? Loc.GetString(bark.Name)
            : Loc.GetString("humanoid-profile-editor-bark-none");
    }

    private void PlayPreviewBark()
    {
        if (Profile is null)
            return;

        _entManager.System<SpeechBarksSystem>().PlayDataPreview(
            Profile.Bark.Proto,
            Profile.Bark.Pitch,
            Profile.Bark.MinVar,
            Profile.Bark.MaxVar);
    }
}
