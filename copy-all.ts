import { Glob } from "bun";
import { readdir } from "node:fs/promises";

const otherDir = process.env["FINAL_COPY"] || undefined;

const plugins = new Glob("*");
for await (const plugin of plugins.scan({ onlyFiles: false })) {
    try {
        await readdir(plugin);

        const artifact = Bun.file(`${plugin}/bin/Release/net8.0/${plugin}.dll`);
        if (!await artifact.exists()) continue;

        // Copy to `dist/artifact` directory.
        await Bun.write(`dist/${plugin}.dll`, artifact);

        if (otherDir) {
            // Copy to `dist/otherDir/artifact` directory.
            await Bun.write(`${otherDir}/${plugin}.dll`, artifact);
        }
    } catch {

    }
}
