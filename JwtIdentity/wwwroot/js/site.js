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

// Returns true if the screen width is typical of a mobile device
function isMobile() {
    return window.innerWidth <= 768;
}

function scrollToElement(id) {
    return new Promise(resolve => {
        const element = document.getElementById(id);
        if (!element) { resolve(); return; }

        // The application uses a scrollable ".main-content" container rather than the
        // document body. "scrollIntoView" on the element may try to scroll the body,
        // which has overflow hidden, resulting in no movement. Explicitly scroll the
        // container if it exists, falling back to the default behaviour otherwise.
        const container = document.querySelector('.main-content');

        const finish = () => {
            if (container) {
                container.removeEventListener('scroll', onScroll);
            }
            resolve();
        };

        let onScroll = null;

        if (container) {
            const rect = element.getBoundingClientRect();
            const containerRect = container.getBoundingClientRect();
            const offset = rect.top - containerRect.top + container.scrollTop;
            const top = offset - container.clientHeight / 2 + rect.height / 2;

            onScroll = () => {
                if (Math.abs(container.scrollTop - top) <= 1) {
                    finish();
                }
            };

            container.addEventListener('scroll', onScroll);
            container.scrollTo({ top, behavior: 'smooth' });

            // Fallback in case the scroll event doesn't fire
            setTimeout(finish, 500);
        } else {
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
            setTimeout(resolve, 500);
        }
    });
}

function loadGoogleAds() {
    if (document.getElementById('google-ads-script')) {
        return; // Avoid loading multiple times
    }

    const script = document.createElement('script');
    script.id = 'google-ads-script';
    script.async = true;
    script.src = 'https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-6296355313447930';
    script.crossOrigin = 'anonymous';

    document.head.appendChild(script);
}

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

function userHasThirdPartyConsent() {
    const consent = getCookie('ThirdPartyCookieConsent');
    return consent === 'True' || consent === 'AllCookies';
}

// Function to set cookie consent through the API
function setThirdPartyCookieConsent(consentType) {
    return fetch(`/api/cookie/consent?consent=${consentType}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to save cookie consent');
            }
            return response;
        })
        .catch(error => {
            console.error('Error saving cookie consent:', error);
        });
}

// Helper functions with success callbacks
function acceptAllCookies(successCallback) {
    setThirdPartyCookieConsent('AllCookies')
        .then(() => {
            if (typeof successCallback === 'function') {
                successCallback();
            }
            // Load third-party services like Google Ads
            if (typeof loadGoogleAds === 'function') {
                loadGoogleAds();
            }
        });
}

function rejectThirdPartyCookies(successCallback) {
    setThirdPartyCookieConsent('EssentialOnly')
        .then(() => {
            if (typeof successCallback === 'function') {
                successCallback();
            }
            // Don't load third-party services
        });
}

// Function to delete a cookie by setting its expiration date to the past
function deleteCookie(name) {
    console.log(`Attempting to delete cookie: ${name}`);
    
    // Try deleting with various path and domain combinations
    const paths = ['/', '', '/api', '.', null];
    const domains = [window.location.hostname, '.' + window.location.hostname, null];
    
    // Try with many combinations of path/domain to ensure deletion
    paths.forEach(path => {
        domains.forEach(domain => {
            // Standard cookie deletion with path/domain
            document.cookie = name + '=' +
                (path ? ';path=' + path : '') +
                (domain ? ';domain=' + domain : '') +
                ';expires=Thu, 01 Jan 1970 00:00:01 GMT';
            
            // Try with secure flag
            document.cookie = name + '=' +
                (path ? ';path=' + path : '') +
                (domain ? ';domain=' + domain : '') +
                ';expires=Thu, 01 Jan 1970 00:00:01 GMT;secure';
            
            // Try with SameSite flags
            const sameSiteValues = ['Lax', 'Strict', 'None'];
            sameSiteValues.forEach(sameSite => {
                document.cookie = name + '=' +
                    (path ? ';path=' + path : '') +
                    (domain ? ';domain=' + domain : '') +
                    ';expires=Thu, 01 Jan 1970 00:00:01 GMT;SameSite=' + sameSite +
                    (sameSite === 'None' ? ';Secure' : '');
            });
        });
    });
    
    // Verify if the cookie was successfully deleted
    const stillExists = document.cookie.split(';').some(cookie => 
        cookie.trim().startsWith(name + '=')
    );
    
    if (stillExists) {
        console.warn(`Failed to delete cookie: ${name}`);
    } else {
        console.log(`Successfully deleted cookie: ${name}`);
    }
}

// Function to identify and delete common third-party cookies
function deleteThirdPartyCookies() {
    console.log('Deleting third-party cookies...');
    
    // Common tracking and analytics cookies - extended list
    const thirdPartyCookieNames = [
        // Google Analytics
        '_ga', '_gid', '_gat', '_ga_', 'AMP_TOKEN', '_gac_',
        // Google Ads and DoubleClick
        'IDE', 'DSID', 'NID', 'ANID', 'CONSENT', 'DV', '1P_JAR', 'APISID', 'HSID',
        'SAPISID', 'SID', 'SIDCC', 'SSID', 'SEARCH_SAMESITE', '__Secure-3PAPISID',
        '__Secure-3PSID', '__Secure-3PSIDCC', 'PREF', 'VISITOR_INFO1_LIVE',
        // Facebook
        '_fbp', 'fr', 'datr', 'c_user', 'xs', 'spin', 'wd', 'sb', 'presence',
        // Other common analytics
        '_hjid', '_hjSessionUser', '_hjSession', '_hjAbsoluteSessionInProgress',
        'amplitude_id', '__hstc', 'hubspotutk', '__hssrc', '__hssc',
        '_mkto_trk', 'intercom-session-*', 'intercom-id-*'
    ];
    
    // Get all cookies
    const cookies = document.cookie.split(';');
    console.log(`Found ${cookies.length} cookies total`);
    
    if (cookies.length > 0) {
        console.log('Current cookies: ' + document.cookie);
    }
    
    // Track essential cookies that should be preserved
    const essentialCookies = ['authToken', 'ThirdPartyCookieConsent'];
    
    // Counter for deleted cookies
    let deletedCount = 0;
    
    // First approach: Delete known third-party cookies by name
    thirdPartyCookieNames.forEach(cookieName => {
        deleteCookie(cookieName);
        deletedCount++;
    });
    
    // Second approach: Check all existing cookies
    cookies.forEach(cookie => {
        const cookieParts = cookie.trim().split('=');
        if (cookieParts.length < 1) return;
        
        const cookieName = cookieParts[0].trim();
        
        // Skip essential cookies
        if (essentialCookies.includes(cookieName)) {
            console.log(`Preserving essential cookie: ${cookieName}`);
            return;
        }
        
        // If it's not in our essential list, delete it to be safe
        if (!essentialCookies.includes(cookieName)) {
            console.log(`Deleting cookie: ${cookieName}`);
            deleteCookie(cookieName);
            deletedCount++;
        }
    });
    
    // Third approach: Use a more aggressive approach to clear all cookies except essential ones
    document.cookie.split(';').forEach(cookie => {
        const cookieParts = cookie.trim().split('=');
        if (cookieParts.length < 1) return;
        
        const cookieName = cookieParts[0].trim();
        
        // Skip essential cookies
        if (essentialCookies.includes(cookieName)) {
            return;
        }
        
        // Delete everything else
        deleteCookie(cookieName);
    });
    
    // Clean up all third-party elements from the DOM
    cleanupThirdPartyDomElements();
    
    console.log(`Attempted to delete ${deletedCount} third-party cookies`);
    console.log('Remaining cookies: ' + document.cookie);
    
    return deletedCount > 0;
}

// Function to clean up third-party DOM elements more thoroughly
function cleanupThirdPartyDomElements() {
    console.log('Cleaning up third-party DOM elements...');
    
    // Remove Google Ads scripts
    const scripts = document.querySelectorAll('script[src*="google"]');
    console.log(`Removing ${scripts.length} Google-related scripts`);
    scripts.forEach(script => script.remove());
    
    // Remove Google Ads iframe
    const googleEsf = document.getElementById('google_esf');
    if (googleEsf) {
        console.log('Removing Google ESF iframe');
        googleEsf.remove();
    }
    
    // Remove AdSense elements
    const adsenseElements = document.querySelectorAll('ins.adsbygoogle, ins.adsbygoogle-noablate');
    console.log(`Removing ${adsenseElements.length} AdSense elements`);
    adsenseElements.forEach(element => element.remove());
    
    // Remove any other ad-related iframes
    const adIframes = document.querySelectorAll('iframe[src*="doubleclick"], iframe[src*="googleads"], iframe[id^="aswift_"]');
    console.log(`Removing ${adIframes.length} ad-related iframes`);
    adIframes.forEach(iframe => iframe.remove());
    
    // Remove Google ad containers
    const adContainers = document.querySelectorAll('div[id^="aswift_"][id$="_host"]');
    console.log(`Removing ${adContainers.length} ad containers`);
    adContainers.forEach(container => container.remove());
    
    // Remove any hidden elements with Google ad attributes
    const hiddenAdElements = document.querySelectorAll('[data-ad-client], [data-ad-slot], [data-ad-format], [data-adsbygoogle-status]');
    console.log(`Removing ${hiddenAdElements.length} hidden ad elements`);
    hiddenAdElements.forEach(element => element.remove());
    
    // Clean up Google Analytics objects
    if (window.ga) {
        console.log('Disabling Google Analytics');
        window.ga = undefined;
    }
    if (window.google_tag_manager) {
        console.log('Disabling Google Tag Manager');
        window.google_tag_manager = undefined;
    }
    if (window.dataLayer) {
        console.log('Clearing dataLayer');
        window.dataLayer = undefined;
    }
    
    console.log('Third-party DOM cleanup complete');
}

// Function to handle cleaning up third-party services completely
function clearThirdPartyServicesCompletely() {
    console.log('Performing complete third-party service cleanup...');
    
    // First call the server-side endpoint to clear cookies (for any cookies that might be accessible to the server)
    fetch('/api/cookie/clear')
        .then(response => response.json())
        .then(data => {
            console.log('Server-side cookie deletion result:', data);
            
            // Clean up DOM elements
            cleanupThirdPartyDomElements();
            
            // Clean up all Google variables to prevent tracking
            if (window.google) {
                console.log('Removing window.google');
                window.google = undefined;
            }
            
            if (window.gaData) {
                console.log('Removing window.gaData');
                window.gaData = undefined;
            }
            
            if (window.gaGlobal) {
                console.log('Removing window.gaGlobal');
                window.gaGlobal = undefined;
            }
            
            if (window.gaplugins) {
                console.log('Removing window.gaplugins');
                window.gaplugins = undefined;
            }
            
            // Clear tracking data from localStorage
            clearTrackingFromStorage();
            
            // Set a flag indicating we want a clean context
            localStorage.setItem('requireCleanContext', 'true');
            
            console.log('Complete third-party service cleanup finished. Reloading for clean context...');
        })
        .catch(error => {
            console.error('Error calling server-side cookie deletion:', error);
        });
}

// Helper function to clear tracking data from storage
function clearTrackingFromStorage() {
    try {
        // Common tracking keys in localStorage
        const trackingKeys = ['_ga', 'google', 'analytics', 'ads', 'tracking'];
        
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            
            // Check if the key matches any tracking pattern
            if (key && trackingKeys.some(trackingKey => 
                key.toLowerCase().includes(trackingKey.toLowerCase()))) {
                console.log(`Removing localStorage item: ${key}`);
                localStorage.removeItem(key);
            }
        }
        
        // Also check sessionStorage
        for (let i = 0; i < sessionStorage.length; i++) {
            const key = sessionStorage.key(i);
            
            // Check if the key matches any tracking pattern
            if (key && key.toLowerCase().includes('google')) {
                console.log(`Removing sessionStorage item: ${key}`);
                sessionStorage.removeItem(key);
            }
        }
    } catch (e) {
        console.error('Error clearing storage items:', e);
    }
}

// Update the clear cookie consent function to use our more thorough approach
function clearCookieConsent() {
    console.log('Clearing cookie consent...');
    
    // Delete the consent cookie itself
    deleteCookie('ThirdPartyCookieConsent');
    
    // Perform complete cleanup of third-party services
    clearThirdPartyServicesCompletely();
    
    console.log('Cookie consent cleared and third-party services disabled');

    return true;
}

// Print the supplied element and all of its contents
function printElement(element) {
    const printWindow = window.open('', '_blank');
    printWindow.document.write('<html><head><title>Print</title>');
    // Include existing head content for styles but exclude scripts
    const headContent = Array.from(document.head.children)
        .filter(node => node.tagName !== 'SCRIPT')
        .map(node => node.outerHTML)
        .join('');
    printWindow.document.write(headContent);
    // Add print specific styles
    printWindow.document.write('<style>@media print { .print-chart { break-after: page; page-break-after: always; } .print-chart:last-child { break-after: avoid; page-break-after: auto; } }</style>');
    printWindow.document.write('</head><body></body></html>');
    printWindow.document.close();

    // Wait for the new window to finish loading before printing
    printWindow.onload = () => {
        serializeElementToJson(element); // For debugging purposes
        console.log('body', printWindow.document.body);
        // Clone the entire element to preserve all charts
        const clone = element.cloneNode(true);
        // Remove any script tags from the clone for safety
        clone.querySelectorAll('script').forEach(script => script.remove());
        // Append the cloned content to the print window's body
        printWindow.document.body.appendChild(clone);

        printWindow.focus();
        printWindow.print();
        printWindow.close();
    };
}

// Create a function to serialize the entire element from the printElement function as json and write it to the console
function serializeElementToJson(element) {
    function serializeNode(node) {
        const obj = {
            nodeType: node.nodeType,
            nodeName: node.nodeName,
        };
        if (node.nodeType === Node.ELEMENT_NODE) {
            obj.attributes = {};
            for (let attr of node.attributes) {
                obj.attributes[attr.name] = attr.value;
            }
            obj.children = [];
            for (let child of node.childNodes) {
                obj.children.push(serializeNode(child));
            }
        } else if (node.nodeType === Node.TEXT_NODE) {
            obj.textContent = node.textContent;
        }
        return obj;
    }
    const serialized = serializeNode(element);
    console.log(JSON.stringify(serialized, null, 2));
    return serialized;
}
