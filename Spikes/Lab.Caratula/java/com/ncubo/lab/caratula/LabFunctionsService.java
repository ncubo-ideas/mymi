package com.ncubo.lab.caratula;

import android.app.appfunctions.AppFunctionException;
import android.app.appfunctions.AppFunctionService;
import android.app.appfunctions.ExecuteAppFunctionRequest;
import android.app.appfunctions.ExecuteAppFunctionResponse;
import android.app.appsearch.GenericDocument;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.content.pm.SigningInfo;
import android.os.Bundle;
import android.os.CancellationSignal;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Message;
import android.os.Messenger;
import android.os.OutcomeReceiver;
import android.os.RemoteException;
import android.util.Log;

/**
 * LA CARATULA DE PLATAFORMA — Puerta 3, spike.
 *
 * Esto es lo que Alvaro llama en su R13 "un shim Android diminuto por modulo, generado por
 * codegen desde el manifiesto tipado". Aca esta escrito a mano para UNA tool: el generador
 * viene despues, si el spike pasa.
 *
 * NO TIENE NADA ADENTRO. Ni dominio, ni repertorio, ni journal, ni semantica. Su unico
 * trabajo es declararle la funcion al sistema operativo y reenviar a Gnothi, que sigue
 * teniendo todo. Si este archivo algun dia sabe algo de facturas, el diseño se rompio.
 *
 * POR QUE ESTA EN JAVA Y NO EN C#: medido. En los bindings de .NET,
 * android/app/appfunctions/AppFunctionService esta marcada DoNotGenerateAcw — no se puede
 * heredar de ella desde C#. Confirma la decision de Alvaro de que la caratula no tiene por
 * que estar en el lenguaje del dominio.
 *
 * EL FORMATO se aprendio de una app que ya funciona en el aparato: se descargo el APK de
 * Samsung Reminder y se leyo su assets/app_function.xml y su manifiesto. No se supuso nada.
 */
public final class LabFunctionsService extends AppFunctionService {

    private static final String TAG = "SPIKE";

    /** El paquete de Gnothi y su service de IPC — el tramo que el Spike 2 ya midio. */
    private static final String GNOTHI_PKG = "com.ncubo.gnothi.host";
    private static final String GNOTHI_SVC = "com.ncubo.gnothi.host.IpcService";

    @Override
    public void onExecuteFunction(
            ExecuteAppFunctionRequest request,
            String callingPackage,
            SigningInfo signingInfo,
            CancellationSignal cancellationSignal,
            OutcomeReceiver<ExecuteAppFunctionResponse, AppFunctionException> receiver) {

        final long t0 = System.currentTimeMillis();
        final String functionId = request.getFunctionIdentifier();

        // LA IDENTIDAD DEL CALLER, que el sistema nos entrega. Es MEJOR que lo que medimos en
        // el Spike 2 (solo uid): aca viene el paquete Y su SigningInfo, que es justo la
        // compuerta gruesa de la cadena de identidad de R13.
        int firmas = (signingInfo == null || signingInfo.getApkContentsSigners() == null)
                ? 0 : signingInfo.getApkContentsSigners().length;
        Log.i(TAG, "caratula: onExecuteFunction " + functionId
                + " · caller=" + callingPackage + " · firmas=" + firmas);

        // El function_id es "<paquete>#<modulo>_<tool>". El shim NO interpreta semantica:
        // solo parte el identificador que el generador escribio.
        String tail = functionId.contains("#")
                ? functionId.substring(functionId.indexOf('#') + 1) : functionId;
        int cut = tail.indexOf('_');
        final String module = cut < 0 ? tail : tail.substring(0, cut);
        final String tool   = cut < 0 ? "" : tail.substring(cut + 1);

        final Context app = getApplicationContext();
        // Una celda para que el callback pueda desligar la MISMA conexion que lo creo.
        final ServiceConnection[] cell = new ServiceConnection[1];

        cell[0] = new ServiceConnection() {
            @Override public void onServiceConnected(ComponentName name, IBinder binder) {
                Log.i(TAG, "caratula: ligado a Gnothi a los " + (System.currentTimeMillis() - t0) + " ms");
                try {
                    Messenger gnothi = new Messenger(binder);

                    Bundle data = new Bundle();
                    data.putString("module", module);
                    data.putString("tool", tool);

                    Message msg = Message.obtain();
                    msg.setData(data);
                    msg.replyTo = new Messenger(new Handler(Looper.getMainLooper()) {
                        @Override public void handleMessage(Message answer) {
                            Bundle d = answer.getData();
                            String json = (d == null) ? "" : d.getString("json", "");
                            Log.i(TAG, "caratula: Gnothi contesto a los "
                                    + (System.currentTimeMillis() - t0) + " ms · " + json);

                            // La respuesta viaja como GenericDocument de AppSearch, en la
                            // propiedad que la plataforma define.
                            GenericDocument doc = new GenericDocument.Builder<>("", "", "")
                                    .setPropertyString(
                                            ExecuteAppFunctionResponse.PROPERTY_RETURN_VALUE, json)
                                    .build();
                            receiver.onResult(new ExecuteAppFunctionResponse(doc));

                            try { app.unbindService(cell[0]); } catch (Exception ignored) { }
                        }
                    });

                    gnothi.send(msg);
                } catch (RemoteException e) {
                    Log.e(TAG, "caratula: fallo el envio", e);
                    receiver.onError(new AppFunctionException(
                            AppFunctionException.ERROR_APP_UNKNOWN_ERROR, String.valueOf(e.getMessage())));
                }
            }

            @Override public void onServiceDisconnected(ComponentName name) {
                Log.i(TAG, "caratula: Gnothi se desconecto");
            }
        };

        Intent intent = new Intent();
        intent.setComponent(new ComponentName(GNOTHI_PKG, GNOTHI_SVC));

        boolean ok = app.bindService(intent, cell[0], Context.BIND_AUTO_CREATE);
        Log.i(TAG, "caratula: bindService=" + ok);
        if (!ok) {
            receiver.onError(new AppFunctionException(
                    AppFunctionException.ERROR_APP_UNKNOWN_ERROR, "no se pudo ligar a Gnothi"));
        }
    }
}
