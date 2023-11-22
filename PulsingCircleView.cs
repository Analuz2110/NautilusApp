using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NautilusApp
{
    internal class PulsingCircleView : ContentView
    {
        public PulsingCircleView()
        {
            // Set up the initial appearance of the circle (e.g., size, color)
            Content = new BoxView
            {
                WidthRequest = 80,
                HeightRequest = 80,
                Color = Color.FromHex("#FF0000"), // Set your desired color
            };

            // Add the pulsing animation
            var scaleAnimation = new Animation(v => Content.Scale = v, 1, 1.5);
            var fadeAnimation = new Animation(v => Content.Opacity = v, 1, 0);

            var pulsingAnimation = new Animation();
            pulsingAnimation.Add(0, 0.5, scaleAnimation);
            pulsingAnimation.Add(0.5, 1, fadeAnimation);

            // Repeat the animation indefinitely
            pulsingAnimation.Commit(this, "PulseAnimation", length: 2000, repeat: () => true);
        }
    }
}
