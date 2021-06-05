using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalismCalendar.Controllers.Navigation
{
    /// <summary>
    /// Base class for app navigation states.
    /// </summary>
    public abstract class AppNavigationState
    {
        // Reference to the object the state is for (its context)
        public AppController Controller { get; set; }

        // Reference to the previous state, for back navigation.
        public AppNavigationState LastState { get; set; }

        // Other states (transition definitions).
        /// <summary>
        /// Transition function from the current state to the Home state.
        /// </summary>
        public abstract void GotoHome();

        /// <summary>
        /// Transition function from the current state to the Settings state.
        /// </summary>
        public abstract void GotoSettings();

        /// <summary>
        /// Transition function to return to the previous state.
        /// </summary>
        public abstract void GoBack();
    }
}
