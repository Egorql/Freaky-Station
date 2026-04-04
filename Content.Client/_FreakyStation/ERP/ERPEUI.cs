using Content.Client.Eui;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._FreakyStation.ERP
{
    [UsedImplicitly]
    public sealed class ERPEUI : BaseEui
    {
        private readonly ERPUI _window;

        public ERPEUI()
        {
            _window = new ERPUI(this);
            _window.OnClose += OnClosed;
        }

        public override void Opened()
        {
            _window.OpenCentered();
        }

        public override void Closed()
        {
            base.Closed();
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            var euiState = (ERPInteractionEuiState) state;
            _window.TargetEntityId = euiState.Target;
            _window.Mode = euiState.Mode;
            _window.CooldownEndTime = euiState.CooldownEndTime;
            _window.UserSex = euiState.UserSex;
            _window.TargetSex = euiState.TargetSex;
            _window.UserHasClothing = euiState.UserHasClothing;
            _window.TargetHasClothing = euiState.TargetHasClothing;
            _window.UserConsent = euiState.UserConsent;
            _window.TargetConsent = euiState.TargetConsent;
            _window.UserNonCon = euiState.UserNonCon;
            _window.TargetNonCon = euiState.TargetNonCon;
            _window.UserArousal = euiState.UserArousal;
            _window.TargetArousal = euiState.TargetArousal;
            _window.Interactions = euiState.Interactions;
            _window.Populate();
        }

        public void OnItemSelect(ItemList.ItemListSelectedEventArgs args)
        {
            var item = args.ItemList[args.ItemIndex];
            if (item.Metadata is not ERPInteractionEntryState interaction)
                return;

            SendMessage(new PerformInteractionMessage(interaction.InteractionId));
        }

        private void OnClosed()
        {
            SendMessage(new CloseEuiMessage());
        }
    }
}
