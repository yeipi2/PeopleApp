window.googleAuth = {
    initialize: function (clientId, dotnetHelper) {
        if (typeof google === 'undefined') {
            console.error('Google Sign-In library not loaded');
            return;
        }

        google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                dotnetHelper.invokeMethodAsync('HandleGoogleResponse', response.credential);
            }
        });
    },

    renderButton: function (elementId) {
        if (typeof google === 'undefined') {
            console.error('Google Sign-In library not loaded');
            return;
        }

        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Element not found:', elementId);
            return;
        }

        google.accounts.id.renderButton(
            element,
            {
                theme: "outline",
                size: "large",
                width: element.offsetWidth || 320,
                text: "continue_with",
                shape: "rectangular",
                logo_alignment: "left"
            }
        );
    }
};