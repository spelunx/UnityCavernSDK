# Spelunx Unity Cavern SDK

## Links
- Project Website: https://projects.etc.cmu.edu/spelunx/
- API Documentation: https://spelunx.github.io/UnityCavernSDK
- User Tutorial + Design Insights: https://docs.google.com/document/d/1QsmDlrw0ctSM0vhB-qM79fx0NRbXh79U3FtuWtl2vYs/edit?usp=sharing
- Camera Implementation: https://drive.google.com/file/d/1EkW47ti7jpWOY_gsrfYsj59GmGbZn8wC/view

## Important Notes
- The `com.spelunx.cavern.orbbec.libs` package will be automatically downloaded as a dependency of `com.spelunx.cavern.orbbec.sdk` (must be connected to Carnegie Mellon University's WLAN). It was not uploaded to GitHub as it contained large binary files.
- When building an executable that uses `com.spelunx.cavern.orbbec.libs`, you need to copy the following files found in `com.spelunx.cavern.orbbec.libs\Libs` into the root folder of your executable:
    - /directml.dll
    - /dnn_model_2_0_op11.onnx
    - /onnxruntime.dll
    - /onnxruntime_providers_cuda.dll
    - /onnxruntime_providers_shared.dll
    - /onnxruntime_providers_tensorrt.dll