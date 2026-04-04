// SPDX-FileCopyrightText: 2025 Egorql <Egorkashilkin@gmail.com>
// SPDX-FileCopyrightText: 2025 ReserveBot <211949879+ReserveBot@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared._FreakyStation.ERP;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Server._FreakyStation.ERP
{
    [UsedImplicitly]
    public sealed class ERPEUI : BaseEui
    {
        private readonly ERPSystem _system;
        private readonly NetEntity _target;
        private readonly ERPPanelMode _mode;

        public ERPEUI(ERPSystem system, NetEntity target, ERPPanelMode mode)
        {
            _system = system;
            _target = target;
            _mode = mode;
        }

        public bool TracksEntity(EntityUid uid)
        {
            return GetTrackedUser() == uid || IoCManager.Resolve<IEntityManager>().GetEntity(_target) == uid;
        }

        public override void Opened()
        {
            base.Opened();
            _system.RegisterEui(this);
            StateDirty();
        }

        public override void Closed()
        {
            base.Closed();
            _system.UnregisterEui(this);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (Player.AttachedEntity is not { } user)
            {
                Close();
                return;
            }

            var target = IoCManager.Resolve<IEntityManager>().GetEntity(_target);

            if (!_system.CanKeepPanelOpen(user, target, _mode))
            {
                Close();
                return;
            }

            switch (msg)
            {
                case PerformInteractionMessage perform:
                    _system.TryPerformInteraction(user, target, _mode, perform.InteractionId);
                    StateDirty();
                    break;
            }
        }

        public override EuiStateBase GetNewState()
        {
            if (Player.AttachedEntity is not { } user)
            {
                Close();
                return new ERPInteractionEuiState();
            }

            var target = IoCManager.Resolve<IEntityManager>().GetEntity(_target);
            if (!_system.CanKeepPanelOpen(user, target, _mode))
            {
                Close();
                return new ERPInteractionEuiState();
            }

            return _system.BuildState(user, target, _mode);
        }

        private EntityUid? GetTrackedUser()
        {
            return Player.AttachedEntity;
        }
    }
}
