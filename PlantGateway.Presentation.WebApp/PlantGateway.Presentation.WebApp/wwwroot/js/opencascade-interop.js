window.OpenCascadeInterop = {
    load: async function () {
        if (!window.oc) {
            window.oc = await opencascade();
        }
        return true;
    },

    loadStep: async function (arrayBuffer) {
        const oc = window.oc;
        const byteArray = new Uint8Array(arrayBuffer);
        const shape = oc.readSTEP(byteArray);
        return !!shape;
    }
};
