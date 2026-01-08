class Viewer3D {

    constructor(canvas) {
        this.canvas = canvas;
        this.gl = canvas.getContext("webgl");

        if (!this.gl) {
            alert("WebGL not supported");
            return;
        }

        // Auto-resize
        this.resize();
        window.addEventListener("resize", () => this.resize());

        // Simple camera state
        this.angle = 0;
        this.shape = null;

        // Start render loop
        requestAnimationFrame(() => this.render());
    }

    resize() {
        const dpr = window.devicePixelRatio || 1;
        const rect = this.canvas.getBoundingClientRect();
        this.canvas.width = rect.width * dpr;
        this.canvas.height = rect.height * dpr;
        this.gl.viewport(0, 0, this.canvas.width, this.canvas.height);
    }

    display(shape) {
        this.shape = shape;
        console.log("STEP Loaded:", shape);
    }

    reset() {
        this.angle = 0;
    }

    render() {
        if (!this.gl) return;

        const gl = this.gl;

        // Clear
        gl.clearColor(0.95, 0.95, 0.95, 1);
        gl.clear(gl.COLOR_BUFFER_BIT);

        // Placeholder animation until real triangulation
        if (this.shape) {
            this.angle += 0.01;
        }

        // Continue loop
        requestAnimationFrame(() => this.render());
    }
}


window.StepViewerInterop = {

    ocInstance: null,
    viewerMap: new Map(),

    loadEngine: async function () {
        if (!window.StepViewerInterop.ocInstance) {
            window.StepViewerInterop.ocInstance = await opencascade();
        }
    },

    loadStepFile: async function (canvas, fileBytes) {

        const oc = window.StepViewerInterop.ocInstance;
        const buffer = new Uint8Array(fileBytes);

        const shape = oc.readSTEP(buffer);

        // Create Viewer3D if not exists
        let viewer = window.StepViewerInterop.viewerMap.get(canvas);
        if (!viewer) {
            viewer = new Viewer3D(canvas);
            window.StepViewerInterop.viewerMap.set(canvas, viewer);
        }

        viewer.display(shape);
    },

    resetView: function (canvas) {
        const viewer = window.StepViewerInterop.viewerMap.get(canvas);
        if (viewer) viewer.reset();
    }
};
