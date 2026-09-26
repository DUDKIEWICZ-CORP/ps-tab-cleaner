// PS Tab Cleaner (macOS) — shows only the file name in Photoshop document tabs.
// (c) Dudkiewicz Corp — dudkiewiczcorp.com
import SwiftUI
import AppKit

struct PSInstall: Identifiable {
    let id = UUID()
    let name: String
    let path: String
    var patched: Bool
}

final class Model: ObservableObject {
    @Published var installs: [PSInstall] = []
    @Published var busy = false
    @Published var message = ""

    func scan() {
        var found: [PSInstall] = []
        let fm = FileManager.default
        let apps = (try? fm.contentsOfDirectory(atPath: "/Applications")) ?? []
        for a in apps.sorted() where a.hasPrefix("Adobe Photoshop") {
            let dir = "/Applications/" + a
            guard let en = fm.enumerator(atPath: dir + "/Locales") else { continue }
            var patched: Bool? = nil
            while let rel = en.nextObject() as? String {
                if rel.contains("tw10428_") && rel.hasSuffix(".dat") {
                    patched = Self.isPatched(dir + "/Locales/" + rel)
                    break
                }
            }
            if let p = patched { found.append(PSInstall(name: a, path: dir, patched: p)) }
        }
        installs = found
    }

    static func isPatched(_ file: String) -> Bool {
        guard let data = FileManager.default.contents(atPath: file) else { return false }
        var text: String? = nil
        if data.count > 2, data[0] == 0xFF, data[1] == 0xFE {
            text = String(data: data.dropFirst(2), encoding: .utf16LittleEndian)
        } else {
            text = String(data: data, encoding: .utf8)
                ?? String(data: data, encoding: .utf16LittleEndian)
        }
        return text?.contains("ImageWindow/TitleTemplate=^0\"") ?? false
    }

    func run(_ mode: String, _ inst: PSInstall) {
        guard let engine = Bundle.main.path(forResource: "engine", ofType: "sh") else {
            message = "engine.sh missing from the app bundle"
            return
        }
        if isPhotoshopRunning() {
            message = "Close Photoshop first, then try again."
            return
        }
        busy = true
        message = ""
        let cmd = "/bin/bash " + shq(engine) + " " + mode + " " + shq(inst.path)
        let script = "do shell script \"" + escAS(cmd) + "\" with administrator privileges"
        DispatchQueue.global().async {
            let p = Process()
            p.launchPath = "/usr/bin/osascript"
            p.arguments = ["-e", script]
            let pipe = Pipe()
            p.standardError = pipe
            p.launch()
            p.waitUntilExit()
            let err = String(data: pipe.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8) ?? ""
            DispatchQueue.main.async {
                self.busy = false
                if p.terminationStatus != 0 && !err.contains("canceled") {
                    self.message = err.trimmingCharacters(in: .whitespacesAndNewlines)
                }
                self.scan()
            }
        }
    }

    func isPhotoshopRunning() -> Bool {
        NSWorkspace.shared.runningApplications.contains {
            ($0.localizedName ?? "").hasPrefix("Adobe Photoshop")
        }
    }

    func shq(_ s: String) -> String { "'" + s.replacingOccurrences(of: "'", with: "'\\''") + "'" }
    func escAS(_ s: String) -> String {
        s.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
    }
}

struct ContentView: View {
    @StateObject var model = Model()

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("Clean Photoshop document tabs.")
                .font(.system(size: 20, weight: .semibold))
            Text("Shows only the file name instead of zoom, layer and color mode info.\nBackups are kept, Restore undoes everything.")
                .font(.system(size: 12))
                .foregroundColor(.secondary)
            if model.installs.isEmpty {
                Text("No Photoshop installation found in /Applications.")
                    .foregroundColor(.secondary)
                    .padding(.vertical, 20)
            }
            ForEach(model.installs) { inst in
                HStack {
                    Text(inst.name).font(.system(size: 13, weight: .semibold))
                    Spacer()
                    Text(inst.patched ? "patched" : "original")
                        .font(.system(size: 11, weight: .semibold))
                        .foregroundColor(inst.patched ? Color(red: 0.22, green: 0.56, blue: 0.94) : .secondary)
                    Button("Fix Tabs") { model.run("fix", inst) }
                        .disabled(model.busy)
                    Button("Restore") { model.run("restore", inst) }
                        .disabled(model.busy)
                }
                .padding(10)
                .background(RoundedRectangle(cornerRadius: 8).fill(Color(NSColor.windowBackgroundColor)))
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.gray.opacity(0.3)))
            }
            if !model.message.isEmpty {
                Text(model.message).font(.system(size: 11)).foregroundColor(.red)
            }
            HStack {
                Link("ADI.ONLINE", destination: URL(string: "https://www.adi.online")!)
                    .font(.system(size: 10, weight: .bold))
                Text("|").foregroundColor(.secondary)
                Link("DUDKIEWICZ CORP", destination: URL(string: "https://www.dudkiewiczcorp.com")!)
                    .font(.system(size: 10, weight: .bold)).foregroundColor(.secondary)
                Spacer()
                Link("BUY ME A CAFFE", destination: URL(string: "https://buymeacoffee.com/adriandudkiewicz")!)
                    .font(.system(size: 10))
            }.padding(.top, 6)
        }
        .padding(24)
        .frame(width: 520)
        .onAppear { model.scan() }
    }
}

@main
struct PSTabCleanerApp: App {
    var body: some Scene {
        WindowGroup("Photoshop Tabs Cleaner") {
            ContentView()
        }
    }
}
