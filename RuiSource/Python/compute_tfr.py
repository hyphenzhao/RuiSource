import json
import sys
import numpy as np
import mne


def main():
    if len(sys.argv) < 2:
        raise RuntimeError("Missing input json path")

    with open(sys.argv[1], "r", encoding="utf-8") as f:
        payload = json.load(f)

    fmin = float(payload["fmin"])
    fmax = float(payload["fmax"])
    fstep = float(payload["fstep"])
    output_path = payload["output_path"]

    freqs = np.arange(fmin, fmax + (fstep / 2.0), fstep)
    if payload["automatic_n_cycles"]:
        n_cycles = freqs / 2.0
    else:
        n_cycles = float(payload["manual_n_cycles"])

    results = []
    for channel in payload["channels"]:
        samples = np.asarray(channel["samples"], dtype=float)
        sfreq = float(channel["sample_rate"])
        channel_name = channel["channel_name"]

        info = mne.create_info(ch_names=[channel_name], sfreq=sfreq, ch_types=["eeg"])
        epochs = mne.EpochsArray(samples[np.newaxis, np.newaxis, :], info, verbose=False)
        power = mne.time_frequency.tfr_morlet(
            epochs,
            freqs=freqs,
            n_cycles=n_cycles,
            return_itc=False,
            average=True,
            verbose=False,
        )

        results.append({
            "channelName": channel_name,
            "frequencies": power.freqs.tolist(),
            "times": power.times.tolist(),
            "power": power.data[0].tolist(),
        })

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump({"results": results}, f)


if __name__ == "__main__":
    main()
