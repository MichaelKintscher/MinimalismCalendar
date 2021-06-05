using MinimalismCalendar.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Controllers.Navigation
{
    /// <summary>
    /// App navigation state corresponding to the home page.
    /// </summary>
    public class HomeState : AppNavigationState
    {
        #region Constructors
        // Must define a constructor with the context, in order to allow
        //      a context to create this state, and set this state's
        //      reference to itself as the context (object that is in this
        //      state)
        public HomeState(AppController controller)
        {
            this.Controller = controller;
            this.LastState = null;
        }

        // Must define a constructor with the state passed in, in order to
        //      allow another state to create this state during a state
        //      transition for the context, and allow that state to give
        //      this one the same context (object that is in this state)
        public HomeState(AppNavigationState state)
        {
            this.Controller = state.Controller;
            this.LastState = state;
        }
        #endregion

        #region State Transition Functions
        public override void GotoHome()
        {
            // Nothing to do, as already in this state.
        }

        public override void GotoSettings()
        {
            // Instruct the view to navigate.
            this.Controller.RootPage.Navigate(PageTypes.Settings);

            // Change the state to the Settings state.
            this.Controller.NavState = new SettingsState(this);
        }

        public override void GoBack()
        {
            // Instruct the view to navigate back.
            this.Controller.RootPage.NavigateBack();

            // Return to the previous state as the new current state.
            this.Controller.NavState = this.LastState;
        }
        #endregion
    }
}
