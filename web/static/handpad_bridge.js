// Register now in Vuplex, or wait for its documented readiness event.
// Ordinary desktop browsers can preview the pages without a VR bridge.
window.onHandPadMessage = function (listener) {
    if (window.vuplex) {
        window.vuplex.addEventListener('message', listener);
    } else {
        window.addEventListener('vuplexready', function () {
            window.vuplex.addEventListener('message', listener);
        }, { once: true });
    }
};
