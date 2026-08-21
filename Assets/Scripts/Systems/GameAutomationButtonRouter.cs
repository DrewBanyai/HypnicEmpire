using UnityEngine.UI;

namespace HypnicEmpire
{
    public sealed class GameAutomationButtonRouter : IGameAutomationButtonRouter
    {
        private readonly UIView_MainGame mainView;
        private readonly UIActionMenuController actionMenuController;
        private readonly UIDevelopmentsMenu developmentsMenu;
        private readonly UIBuildingsMenu buildingsMenu;

        public GameAutomationButtonRouter(
            UIView_MainGame mainView,
            UIActionMenuController actionMenuController,
            UIDevelopmentsMenu developmentsMenu,
            UIBuildingsMenu buildingsMenu)
        {
            this.mainView = mainView;
            this.actionMenuController = actionMenuController;
            this.developmentsMenu = developmentsMenu;
            this.buildingsMenu = buildingsMenu;
        }

        public GameAutomationClickResult TryClick(string buttonId)
        {
            if (TryResolve(buttonId, out Button button))
            {
                if (button == null || !button.enabled || !button.gameObject.activeInHierarchy || !button.interactable)
                    return GameAutomationClickResult.Unavailable;

                button.onClick.Invoke();
                return GameAutomationClickResult.Clicked;
            }

            //  Buildings are clicked as whole boxes rather than as a Button, so they are purchased through
            //  the same call the UI makes rather than through onClick.
            if (BuildingDataSystem.GetBuildingData(buttonId) != null)
            {
                UIBuildingButton buildingButton = buildingsMenu != null ? buildingsMenu.FindBuildingButton(buttonId) : null;
                if (buildingButton == null || !buildingButton.gameObject.activeInHierarchy)
                    return GameAutomationClickResult.Unavailable;

                if (!BuildingPurchaseSystem.Build(buttonId))
                    return GameAutomationClickResult.Unavailable;

                return GameAutomationClickResult.Clicked;
            }

            return GameAutomationClickResult.UnknownButton;
        }

        private bool TryResolve(string buttonId, out Button button)
        {
            if (mainView != null && mainView.TryGetAutomationButton(buttonId, out button))
                return true;

            if (actionMenuController != null &&
                actionMenuController.TryGetAutomationActionButton(buttonId, out button))
                return true;

            //  A development is named by its full authored title. Its entry only exists once the development
            //  has been offered, so one that has not appeared yet is a button to be waited on rather than a
            //  name that does not exist.
            if (IsDevelopmentTitle(buttonId))
            {
                button = developmentsMenu != null ? developmentsMenu.FindPurchaseButton(buttonId) : null;
                return true;
            }

            button = null;
            return false;
        }

        private static bool IsDevelopmentTitle(string buttonId)
        {
            foreach (var development in DevelopmentSystem.DevelopmentEntries)
                if (development != null && development.Title == buttonId)
                    return true;

            return false;
        }
    }
}
