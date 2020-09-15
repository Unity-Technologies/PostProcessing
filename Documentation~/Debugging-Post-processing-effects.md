# Debugging Post-processing effects

The **Post-process Debug** component displays real-time data about post-processing effects. You can use this data to debug your post-processing effects, and to see the results of adjusting your effects in real time.

When you attach the Post-Process Debug component to a GameObject with a Camera component, the Post-Process Debug component displays its data on top of that Camera's output. You can use the Post-process Debug component in the Unity Editor, or when your application is running on a device.

![img](https://lh4.googleusercontent.com/WC4CXG9PIhEjfZghmKNKtCph-yz83-JULF3FhhFn26JXym15y846WWvF5l2m2aBTfnbG4w0PkOLJNYiD-764W2cxHORDXYJhf0jkcvvdLf17YWlzZnBI-aRd3j8f68WNQP6sx1Wc)The **Histogram** monitor as it appears in the Game window. 

## Using the Post-Process Debug component

1. Create the **Post-process Debug** component on a GameObject. To do this, go to the Inspector window and select **Add component** > **Post-process Debug.**2. Use **Post Process Layer** to choose the post-processing layer that you want to view debug information for. If you create the Post-process Debug component on the Main Camera, Unity automatically assigns Main Camera to this field.2. To view an overlay that shows the state of an intermediate pass:
a. Select an intermediate pass from the **Debug Overlay** drop-down menu. Unity displays the overlay in the Game window.
3. To use the Light Meter Monitor:
a. Enable [**Light Meter**](#light-meter).
b. Unity displays a logarithmic histogram in the Game view that represents exposure. If you have the [**Auto Exposure**](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest?subfolder=/manual/Auto-Exposure.html) post processing effect enabled, the histogram contains a thin line that indicates the current exposure. There is also a translucent area between the maximum and minimum exposure values. You can configure these exposure values in the **Auto Exposure** effect. The result of your changes appear in the Light Meter monitor in real-time.
4. To view tonemapping curves in the Light Meter histogram, enable **Show Curves**.
6. To use the Histogram monitor:
	a. Enable [**Histogram**](#histogram).
	b. Unity displays a linear Gamma histogram in the Game view that represents exposure data in real-time in the rendered image. **Histogram** displays exposure data in more detail than **Light Meter**.
c. Use the **Channel** drop-down menu to select the color data the Histogram displays.
7. To use Waveform monitor:
    a. Enable [**Waveform**](#waveform)
    b. Unity displays a waveform in the Game view which represents the full spectrum of Luma (brightness) data in real-time in the rendered image.

## Post-Process Debug Inspector reference

This section describes the settings that the Unity Editor displays in the Inspector for the **Post-Process Debug** component.

**![img](https://lh6.googleusercontent.com/a6OUvrj73gsZlf5NSQ2tx3fbCn2v60U2lEx3WBuIvknEsHyrA3ToGbyoF-MW6cqaLOlb8FdtZjHP6k-8LSRI1V0puzKzhFZGhZTVeeW-iUW5SUCjmd8A_XMusEtr-c8IaqtxOUzQ)**
| **Property**       | **Description** |
|--------------------|-----------------|
| Post Process Layer | Select the post-processing layer that the Post-process Debug component uses.|

### Overlay

**Overlay** determines the intermediate pass that Unity displays in the Game window.

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| Debug overlay | Set the overlay view. Choose between different key intermediate passes to observe the state at this point in the frame. |

## Monitors

**Monitors** allow you to view data that Unity collects from the Game window in real-time.

<a name="light-meter"></a>
### Light meter
The **Light Meter monitor** displays a logarithmic histogram in the Game window that represents exposure in the Camera’s output. You can control the exposure using the [**Auto Exposure**](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest?subfolder=/manual/Auto-Exposure.html) component. When you configure the exposure values, Unity creates two new bars on the histogram:

- A thin pink bar that indicates the current exposure value.
- A translucent blue bar that indicates the area between the maximum and minimum exposure values.

**![img](https://lh3.googleusercontent.com/2qT6Jpcw6MRzTZ9rBEE6PRaDlG7guSoAYDFDGlIIbwWjSxiphicZoUT9BR_SHahJB0T3R3uP-7j5E84x1bG1SczKkNmpeWijRez-LwE-D8bnFG8aM4czTrCJC-dSo0WSW6RtTcMX)**

The Light meter histogram that Unity shows in the Game window.

| **Property** | **Description**                                                                             |
|--------------|---------------------------------------------------------------------------------------------|
| Light meter  | Enable this checkbox to display the **Light meter** monitor in the Game window.             |
| Show Curves  | Enable this checkbox to display tonemapping curves monitor in the **Light meter** histogram |

<a name="histogram"></a>
### Histogram
The **Histogram** monitor displays a gamma histogram in the Game. A histogram graphs the number of pixels at each color intensity level, to illustrate how pixels in an image are distributed. It can help you determine whether an image is properly exposed or not.

**![img](https://lh4.googleusercontent.com/DwsclJsBsMoFhASqKTM8cbRt6v9QYOJdxvMuMVtTT7zJVnMU8S_DEIohCp4BeCVzRVmOcIO7twD-3MQ1J8qh92CKMuKNWpweNJGQhScuG41TOeAQkrhW2TxA3_d7aA6qKn5R55qc)**

The **Histogram** that Unity shows in the Game window.

| **Property** | **Description** |
|--------------|-----------------|
| Histogram    |Enable this checkbox to display the **Histogram** monitor in the Game window.|
| Channel      | Select the color value.|

<a name="waveform"></a>
### Waveform

The **Waveform** monitor displays the full range of luma (brightness) information in the Camera’s output. Unity displays the Waveform in the Game window. The horizontal axis of the graph corresponds to the render (from left to right) and the vertical axis is the brightness value.

**![img](https://lh3.googleusercontent.com/vCv47DrKOhpAL47E9FdiNarOsied6Jn3czGT7qgWIEUaDYDM87h_zcib68WIAJ9-TK1B1uQTNMSWsyePFRoUZReT0ygSfY6vG0aZyLBD4bDur5fL_3_8x4Ui6U4NXw_-gxyofdbL)**

The **Waveform** that Unity shows in the Game window.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| Waveform     | Enable this checkbox to display the **Waveform** monitor in the Game window. |
| Exposure     | Set the exposure value this graph samples from.              |
<a name="vectorscope"></a>
### **Vectorscope**

The **Vectorscope** monitor measures the overall range of hue and saturation within the Camera’s image in real-time. To display the data, it uses a scatter graph relative to the center of the Vectorscope.

The Vectorscope measures hue values between yellow, red, magenta, blue, cyan and green. The center of the Vectorscope represents absolute zero saturation and the edges represent the highest level of saturation. To determine the hues in your scene and their saturation, look at the distribution of the Vectorscope’s scatter graph.

To identify whether there is a color imbalance in the image, look at how close the middle of the Vectorscope graph is to the absolute center. If the Vectorscope graph is off-center, this indicates that there is a color cast (tint) in the image.

**![img](https://lh6.googleusercontent.com/RPh4fQGvSARBMtRTN0JrA-6vHPsxNDvSlasP2V3qKkRDAeWBUKr-frRngl246bbxL789pOaQxrNVUei4Y7ABodNnQ2eHgdZOZ9PC4ng6gVydRKSWvIZBmUrn6qu6QmkRlRvNbyOa)**

The **Vectorscope** that Unity displays in the Game window.

| **Property** | **Description** |
|--------------|-----------------|
| Vectorscope  | Enable this checkbox to display the **Vectorscope** monitor in the Game window.|
| Exposure     |Set the exposure value this graph samples from.|