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
    
    // Never delete essential cookies
    if (isEssentialCookie(name)) {
        console.log(`Preserving essential cookie: ${name}`);
        return;
    }
    
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

function isEssentialCookie(name) {
    // Preserve auth and antiforgery cookies; names may vary
    if (!name) return false;
    const essentials = [
        'authToken',
        'ThirdPartyCookieConsent',
        '.AspNetCore.Cookies',
        'RequestVerificationToken',
        '__RequestVerificationToken'
    ];
    if (essentials.some(e => e.toLowerCase() === name.toLowerCase())) return true;
    if (name.startsWith('.AspNetCore.Antiforgery')) return true;
    return false;
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
    const essentialCookies = ['authToken', 'ThirdPartyCookieConsent', '.AspNetCore.Cookies', 'RequestVerificationToken', '__RequestVerificationToken'];
    
    // Counter for deleted cookies
    let deletedCount = 0;
    
    // First approach: Delete known third-party cookies by name
    thirdPartyCookieNames.forEach(cookieName => {
        if (isEssentialCookie(cookieName)) return;
        deleteCookie(cookieName);
        deletedCount++;
    });
    
    // Second approach: Check all existing cookies
    cookies.forEach(cookie => {
        const cookieParts = cookie.trim().split('=');
        if (cookieParts.length < 1) return;
        
        const cookieName = cookieParts[0].trim();
        
        // Skip essential cookies
        if (isEssentialCookie(cookieName)) {
            console.log(`Preserving essential cookie: ${cookieName}`);
            return;
        }
        
        // If it's not in our essential list, and looks like third-party, delete it
        const looksThirdParty = thirdPartyCookieNames.some(name => cookieName.startsWith(name));
        if (looksThirdParty) {
            console.log(`Deleting cookie: ${cookieName}`);
            deleteCookie(cookieName);
            deletedCount++;
        }
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
    
    // Perform complete third-party services cleanup
    clearThirdPartyServicesCompletely();
    
    console.log('Cookie consent cleared and third-party services disabled');

    return true;
}

(function () {

    function buildPrintCss() {
        return `
      @page { size: auto; margin: 12.7mm; } /* ~0.5in */

      html, body { height: auto; }
      body { margin: 0; -webkit-print-color-adjust: exact; print-color-adjust: exact; }

      /* Wrap that will contain our cloned charts */
      #AllChartsPrint { width: 100%; margin: 0; padding: 0; }

      /* Turn off flex during print */
      #AllChartsPrint .d-flex { display: block !important; gap: 0 !important; }

      /* Exactly one chart per page */
      #AllChartsPrint .print-chart {
        break-inside: avoid !important;         /* modern */
        page-break-inside: avoid !important;     /* legacy */
        margin: 0 0 12.7mm 0 !important;
      }
      /* Start every *subsequent* chart on a new page */
      #AllChartsPrint .print-chart + .print-chart {
        break-before: page !important;           /* modern */
        page-break-before: always !important;    /* legacy */
      }

      /* Syncfusion specifics: prevent title/subtitle cropping */
      #AllChartsPrint .e-chart { overflow: visible !important; height: auto !important; }
      #AllChartsPrint svg { overflow: visible !important; }

      /* (Optional) lock a predictable chart height */
      /* #AllChartsPrint .e-chart svg { height: 450px !important; } */
    `;
    }

    function cloneStylesInto(head, fromDoc) {
        // Keep relative URLs correct inside the iframe
        const base = fromDoc.createElement('base');
        base.href = document.baseURI;
        head.appendChild(base);

        // Copy <link rel="stylesheet"> and <style> so theme/fonts load
        const nodes = Array.from(document.querySelectorAll('link[rel="stylesheet"], style'));
        nodes.forEach(n => head.appendChild(n.cloneNode(true)));

        // Add our print-only stylesheet
        const style = fromDoc.createElement('style');
        style.textContent = buildPrintCss();
        head.appendChild(style);
    }

    function readyWhenStylesLoaded(doc, cb) {
        const links = Array.from(doc.querySelectorAll('link[rel="stylesheet"]'));
        if (links.length === 0) return cb();
        let remaining = links.length;
        links.forEach(l => {
            const done = () => { if (--remaining === 0) cb(); };
            l.addEventListener('load', done);
            l.addEventListener('error', done);
        });
    }

    window.printPage = function printPage() {
        const source = document.getElementById('PrintArea');
        if (!source) { window.print(); return; }

        // Create an offscreen iframe just for printing
        const iframe = document.createElement('iframe');
        iframe.style.position = 'fixed';
        iframe.style.right = '0';
        iframe.style.bottom = '0';
        iframe.style.width = '0';
        iframe.style.height = '0';
        iframe.style.border = '0';
        iframe.setAttribute('aria-hidden', 'true');
        document.body.appendChild(iframe);

        const doc = iframe.contentDocument || iframe.contentWindow.document;

        // Basic HTML skeleton
        doc.open();
        doc.write('<!doctype html><html><head></head><body></body></html>');
        doc.close();

        // Head: copy styles + add our print CSS
        cloneStylesInto(doc.head, doc);

        // Body: clone the charts
        const wrapper = doc.createElement('div');
        wrapper.id = 'AllChartsPrint';
        // Deep clone, keep inline styles/attributes intact
        const clone = source.cloneNode(true);
        // De-dupe the id to avoid collisions inside iframe (not strictly required but tidy)
        clone.id = 'AllChartsPrintContent';
        wrapper.appendChild(clone);
        doc.body.appendChild(wrapper);

        // When stylesheets are ready, print
        readyWhenStylesLoaded(doc, () => {
            // Give layout a tick, then print
            setTimeout(() => {
                iframe.contentWindow.focus();
                iframe.contentWindow.print();

                // Clean up after printing (works in Chromium/Edge)
                const cleanup = () => {
                    iframe.remove();
                    window.removeEventListener('focus', cleanup);
                };
                iframe.contentWindow.addEventListener('afterprint', cleanup, { once: true });

                // Fallback cleanup if 'afterprint' doesn't fire (older Safari)
                setTimeout(cleanup, 5000);
            }, 50);
        });
    };

})();
