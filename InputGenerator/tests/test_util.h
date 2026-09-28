#pragma once
#include <filesystem>
#include <fstream>
#include <sstream>
#include <string>
#include <atomic>
#include <unistd.h>

// Per-test scratch directory under the system temp dir, removed on destruction.
class TempDir {
public:
    TempDir() {
        static std::atomic<int> counter{0};
        path_ = std::filesystem::temp_directory_path() /
                ("aptree-ig-" + std::to_string(::getpid()) + "-" + std::to_string(counter++));
        std::filesystem::create_directories(path_);
    }
    ~TempDir() { std::error_code ec; std::filesystem::remove_all(path_, ec); }

    std::string write(const std::string& name, const std::string& content) const {
        auto p = path_ / name;
        std::ofstream(p) << content;
        return p.string();
    }
    std::string file(const std::string& name) const { return (path_ / name).string(); }

    static std::string read(const std::string& path) {
        std::ifstream in(path);
        std::stringstream ss;
        ss << in.rdbuf();
        return ss.str();
    }

private:
    std::filesystem::path path_;
};

inline int countOccurrences(const std::string& text, const std::string& needle) {
    int n = 0;
    for (size_t pos = text.find(needle); pos != std::string::npos; pos = text.find(needle, pos + needle.size())) ++n;
    return n;
}
