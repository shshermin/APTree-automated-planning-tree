#!/usr/bin/env python3
"""Plot action-node additions and removals with and without replanning."""

from pathlib import Path
import re

import matplotlib.pyplot as plt
import pandas as pd


SCRIPT_DIR = Path(__file__).resolve().parent
WITHOUT_REPLANNING = (
    SCRIPT_DIR
    / "version1_without_replanning_2026-07-01"
    / "PlannerCalls_2026-07-01_12-25-47.csv"
)
WITH_REPLANNING = (
    SCRIPT_DIR
    / "full_run_2026-07-06_17-25-45"
    / "PlannerCalls_2026-07-06_17-25-45.csv"
)
REPLANNING_EVENTS = (
    SCRIPT_DIR
    / "full_run_2026-07-06_17-25-45"
    / "PRRSummary_2026-07-06_17-25-45.csv"
)
WITH_REPLANNING_ACTIONS = (
    SCRIPT_DIR
    / "version2_with_replanning_2026-07-01"
    / "MLActionResult_2026-07-01_14-05-19.log"
)
WITHOUT_REPLANNING_ACTIONS = (
    SCRIPT_DIR
    / "version1_without_replanning_2026-07-01"
    / "MLActionResult_2026-07-01_12-20-47.log"
)
BIN_SECONDS = 10


def load_additions(path: Path) -> pd.DataFrame:
    frame = pd.read_csv(path)
    frame["Timestamp"] = pd.to_datetime(frame["Timestamp"])
    frame["ActionsGenerated"] = pd.to_numeric(
        frame["ActionsGenerated"], errors="coerce"
    ).fillna(0)
    return frame.loc[frame["Success"].astype(str).str.lower() == "true"].copy()


def load_removals(path: Path) -> pd.DataFrame:
    frame = pd.read_csv(path)
    frame = frame[pd.to_numeric(frame["ReplanNumber"], errors="coerce").notna()].copy()
    frame["Timestamp"] = pd.to_datetime(frame["Timestamp"])
    frame["NodesReplaced"] = pd.to_numeric(
        frame["NodesReplaced"], errors="coerce"
    ).fillna(0)
    return frame


def load_action_deltas(path: Path) -> list[float]:
    pattern = re.compile(r"\[\d+\]\s+\[(\d{2}:\d{2}:\d{2}\.\d{3})\]")
    timestamps = []
    with path.open("r", encoding="utf-8") as log_file:
        for line in log_file:
            match = pattern.search(line)
            if match:
                timestamps.append(pd.to_datetime(match.group(1), format="%H:%M:%S.%f"))
    return [
        (timestamps[index] - timestamps[index - 1]).total_seconds() * 1000
        for index in range(1, len(timestamps))
    ]


def aggregate_events(
    additions: pd.DataFrame, removals: pd.DataFrame | None = None
) -> tuple[pd.DataFrame, int, int]:
    start = additions["Timestamp"].min()
    addition_seconds = (additions["Timestamp"] - start).dt.total_seconds()
    additions = additions.assign(
        Bin=(addition_seconds // BIN_SECONDS).astype(int) * BIN_SECONDS
    )
    added = additions.groupby("Bin")["ActionsGenerated"].sum()

    removed_total = 0
    if removals is not None:
        removal_seconds = (removals["Timestamp"] - start).dt.total_seconds()
        removals = removals.assign(
            Bin=(removal_seconds.clip(lower=0) // BIN_SECONDS).astype(int)
            * BIN_SECONDS
        )
        removed = removals.groupby("Bin")["NodesReplaced"].sum()
        removed_total = int(removals["NodesReplaced"].sum())
    else:
        removed = pd.Series(dtype=float)

    last_bin = int(max(added.index.max(), removed.index.max() if not removed.empty else 0))
    bins = pd.Index(range(0, last_bin + BIN_SECONDS, BIN_SECONDS), name="Bin")
    events = pd.DataFrame(index=bins)
    events["Added"] = added.reindex(bins, fill_value=0)
    events["Removed"] = removed.reindex(bins, fill_value=0)
    events["Net"] = (events["Added"] - events["Removed"]).cumsum()
    return events, int(additions["ActionsGenerated"].sum()), removed_total


def build_net_timeline(
    additions: pd.DataFrame, removals: pd.DataFrame | None = None
) -> pd.DataFrame:
    start = additions["Timestamp"].min()
    added_events = additions[["Timestamp", "ActionsGenerated"]].rename(
        columns={"ActionsGenerated": "Change"}
    )

    event_frames = [added_events]
    if removals is not None:
        removed_events = removals[["Timestamp", "NodesReplaced"]].rename(
            columns={"NodesReplaced": "Change"}
        )
        removed_events["Change"] *= -1
        event_frames.append(removed_events)

    timeline = pd.concat(event_frames, ignore_index=True)
    timeline = timeline.groupby("Timestamp", as_index=False)["Change"].sum()
    timeline = timeline.sort_values("Timestamp")
    timeline["ElapsedMinutes"] = (
        timeline["Timestamp"] - start
    ).dt.total_seconds() / 60
    timeline["Net"] = timeline["Change"].cumsum()

    origin = pd.DataFrame(
        {"Timestamp": [start], "Change": [0], "ElapsedMinutes": [0.0], "Net": [0]}
    )
    return pd.concat([origin, timeline], ignore_index=True)


def plot_panel(
    axis: plt.Axes,
    events: pd.DataFrame,
    title: str,
    added_total: int,
    removed_total: int,
) -> None:
    minutes = events.index.to_numpy() / 60
    bar_width = BIN_SECONDS / 60 * 0.82

    axis.bar(
        minutes,
        events["Added"],
        width=bar_width,
        color="#2878B5",
        alpha=0.78,
        label=f"Added ({added_total:,} total)",
    )
    if removed_total:
        axis.bar(
            minutes,
            -events["Removed"],
            width=bar_width,
            color="#D9534F",
            alpha=0.78,
            label=f"Removed ({removed_total:,} total)",
        )

    axis.axhline(0, color="#454545", linewidth=0.8)
    axis.set_title(title, fontsize=11, fontweight="bold")
    axis.set_xlabel("Elapsed time (min)")
    axis.set_ylabel(f"Action-node change per {BIN_SECONDS} s")
    axis.grid(axis="y", color="#D8D8D8", linewidth=0.6, alpha=0.7)

    net_axis = axis.twinx()
    net_axis.plot(
        minutes,
        events["Net"],
        color="#222222",
        linewidth=1.8,
        label=f"Cumulative net ({int(events['Net'].iloc[-1]):,})",
    )
    net_axis.set_ylabel("Cumulative net action nodes")

    handles, labels = axis.get_legend_handles_labels()
    net_handles, net_labels = net_axis.get_legend_handles_labels()
    axis.legend(handles + net_handles, labels + net_labels, loc="upper left", fontsize=8)


def plot_net_comparison(
    without_timeline: pd.DataFrame, with_timeline: pd.DataFrame
) -> None:
    figure, axis = plt.subplots(figsize=(12, 5.5), constrained_layout=True)
    axis.step(
        without_timeline["ElapsedMinutes"],
        without_timeline["Net"],
        where="post",
        color="#D97706",
        linewidth=2.0,
        label="Without replanning",
    )
    axis.step(
        with_timeline["ElapsedMinutes"],
        with_timeline["Net"],
        where="post",
        color="#2878B5",
        linewidth=1.5,
        label="With replanning",
    )
    axis.set_title(
        "Net action-node count during execution",
        fontsize=14,
        fontweight="bold",
    )
    axis.set_xlabel("Elapsed time (min)")
    axis.set_ylabel("Net action nodes added")
    axis.grid(color="#D8D8D8", linewidth=0.6, alpha=0.7)
    axis.legend(loc="lower right", frameon=True)
    axis.set_xlim(left=0)
    axis.set_ylim(bottom=0)

    output_png = SCRIPT_DIR / "NodeGrowth_net_line_with_without_replanning.png"
    output_pdf = SCRIPT_DIR / "NodeGrowth_net_line_with_without_replanning.pdf"
    figure.savefig(output_png, dpi=200, bbox_inches="tight")
    figure.savefig(output_pdf, bbox_inches="tight")
    plt.close(figure)
    print(f"Saved {output_png}")
    print(f"Saved {output_pdf}")


def plot_compact_combined(
    without_timeline: pd.DataFrame,
    with_timeline: pd.DataFrame,
    without_deltas: list[float],
    with_deltas: list[float],
) -> None:
    figure, (latency_axis, growth_axis) = plt.subplots(
        1, 2, figsize=(14, 3.6), constrained_layout=True
    )

    latency_axis.scatter(
        range(len(with_deltas)),
        with_deltas,
        color="#2878B5",
        alpha=0.58,
        s=9,
        edgecolors="none",
        label="With replanning",
    )
    latency_axis.scatter(
        range(len(without_deltas)),
        without_deltas,
        color="#D97706",
        alpha=0.58,
        s=9,
        edgecolors="none",
        label="Without replanning",
    )
    latency_axis.set_title("(a) Consecutive-action latency", fontweight="bold")
    latency_axis.set_xlabel("Action transition index")
    latency_axis.set_ylabel("Time to next action (ms)")
    latency_axis.grid(color="#D8D8D8", linewidth=0.6, alpha=0.7)
    latency_axis.legend(fontsize=8)

    growth_axis.step(
        without_timeline["ElapsedMinutes"],
        without_timeline["Net"],
        where="post",
        color="#D97706",
        linewidth=2.0,
        label="Without replanning",
    )
    growth_axis.step(
        with_timeline["ElapsedMinutes"],
        with_timeline["Net"],
        where="post",
        color="#2878B5",
        linewidth=1.4,
        label="With replanning",
    )
    growth_axis.set_title("(b) Net action-node count", fontweight="bold")
    growth_axis.set_xlabel("Elapsed time (min)")
    growth_axis.set_ylabel("Net action nodes added")
    growth_axis.set_xlim(left=0)
    growth_axis.set_ylim(bottom=0)
    growth_axis.grid(color="#D8D8D8", linewidth=0.6, alpha=0.7)
    growth_axis.legend(loc="lower right", fontsize=8)

    output_png = SCRIPT_DIR / "ActionLatency_and_NodeGrowth_combined.png"
    output_pdf = SCRIPT_DIR / "ActionLatency_and_NodeGrowth_combined.pdf"
    figure.savefig(output_png, dpi=200, bbox_inches="tight")
    figure.savefig(output_pdf, bbox_inches="tight")
    plt.close(figure)
    print(f"Saved {output_png}")
    print(f"Saved {output_pdf}")


def main() -> None:
    without_additions = load_additions(WITHOUT_REPLANNING)
    with_additions = load_additions(WITH_REPLANNING)
    removals = load_removals(REPLANNING_EVENTS)

    without_events, without_added, without_removed = aggregate_events(
        without_additions
    )
    with_events, with_added, with_removed = aggregate_events(with_additions, removals)
    without_timeline = build_net_timeline(without_additions)
    with_timeline = build_net_timeline(with_additions, removals)
    without_deltas = load_action_deltas(WITHOUT_REPLANNING_ACTIONS)
    with_deltas = load_action_deltas(WITH_REPLANNING_ACTIONS)

    plt.rcParams.update(
        {
            "font.family": "DejaVu Sans",
            "axes.spines.top": False,
            "axes.titlepad": 9,
        }
    )
    figure, axes = plt.subplots(2, 1, figsize=(12, 8), constrained_layout=True)
    plot_panel(
        axes[0],
        without_events,
        "(a) Without replanning",
        without_added,
        without_removed,
    )
    plot_panel(
        axes[1],
        with_events,
        "(b) With replanning",
        with_added,
        with_removed,
    )
    figure.suptitle(
        "Action-node growth with and without replanning",
        fontsize=14,
        fontweight="bold",
    )

    output_png = SCRIPT_DIR / "NodeGrowth_with_without_replanning.png"
    output_pdf = SCRIPT_DIR / "NodeGrowth_with_without_replanning.pdf"
    figure.savefig(output_png, dpi=200, bbox_inches="tight")
    figure.savefig(output_pdf, bbox_inches="tight")
    plt.close(figure)

    plot_net_comparison(without_timeline, with_timeline)
    plot_compact_combined(
        without_timeline,
        with_timeline,
        without_deltas,
        with_deltas,
    )

    print(
        f"Without replanning: +{without_added}, -{without_removed}, "
        f"net {int(without_events['Net'].iloc[-1])}"
    )
    print(
        f"With replanning: +{with_added}, -{with_removed}, "
        f"net {int(with_events['Net'].iloc[-1])}"
    )
    print(
        f"Action transitions: {len(with_deltas)} with replanning, "
        f"{len(without_deltas)} without replanning"
    )
    print(f"Saved {output_png}")
    print(f"Saved {output_pdf}")


if __name__ == "__main__":
    main()