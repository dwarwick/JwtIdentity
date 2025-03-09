// When the reCAPTCHA challenge is solved, call the instance method.
function onCaptchaSuccess(token) {
    if (window.dotnetHelper) {
        window.dotnetHelper.invokeMethodAsync('ReceiveCaptchaToken', token)
            .catch(error => console.error(error));
    } else {
        console.error("DotNet helper not registered.");
    }
}

function renderReCaptcha(containerId, siteKey) {
    var container = document.getElementById(containerId);
    if (!container) {
        console.error("reCAPTCHA container not found.");
        return;
    }

    // Clear the container before rendering to prevent the "must be empty" error
    container.innerHTML = "";

    if (typeof grecaptcha !== 'undefined') {
        grecaptcha.render(containerId, {
            'sitekey': siteKey,
            'callback': onCaptchaSuccess // Ensure this function is defined
        });
    } else {
        console.error('reCAPTCHA API not loaded.');
    }
}

// This function registers the DotNet object reference if you prefer an instance method.
function registerCaptchaCallback(dotnetHelper) {
    window.dotnetHelper = dotnetHelper;
}