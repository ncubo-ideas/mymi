package com.ncubo.lab.octopus;

import android.app.Service;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.os.Binder;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Message;
import android.os.Messenger;
import android.os.Parcel;
import android.os.RemoteException;
import android.util.Log;

/**
 * LA EJECUCION — la mitad de abajo del protocolo "octopus".
 *
 * Contrato reconstruido del APK de produccion (ver NotasGnothiHost/20):
 *
 *   Claude:  bindService(Intent("…tool.EXECUTE_TOOL").setPackage(nuestro))
 *            parcel.writeInterfaceToken(SVC_DESC)
 *            parcel.writeString(toolName)
 *            parcel.writeString(argumentsJson)
 *            parcel.writeStrongInterface(callback)
 *            binder.transact(1, parcel, null, FLAG_ONEWAY)
 *
 *   Nosotros: callback.transact(1, [CB_DESC, id, resultado, exito?1:0], null, FLAG_ONEWAY)
 *
 * El AIDL se implementa a mano sobre Binder crudo: no tenemos el .aidl de Anthropic, y
 * escribir el Parcel con la misma forma es todo lo que hace falta.
 *
 * ADENTRO NO HAY NADA. Ni dominio, ni journal, ni semantica. Parte el nombre publico y
 * reenvia al IpcService de Gnothi, igual que la caratula de AppFunctions.
 */
public final class ToolExecService extends Service {

    private static final String TAG = "OCTOPUS";

    private static final String SVC_DESC = "com.anthropic.claude.tool.thirdparty.IToolExecutionService";
    private static final String CB_DESC  = "com.anthropic.claude.tool.thirdparty.IToolExecutionCallback";

    private static final String GNOTHI_PKG = "com.ncubo.gnothi.host";
    private static final String GNOTHI_SVC = "com.ncubo.gnothi.host.IpcService";

    private final Binder binder = new Binder() {
        @Override
        protected boolean onTransact(int code, Parcel data, Parcel reply, int flags)
                throws RemoteException {

            if (code == INTERFACE_TRANSACTION) {
                reply.writeString(SVC_DESC);
                return true;
            }

            if (code == 1) {
                data.enforceInterface(SVC_DESC);
                String toolName = data.readString();
                String argsJson = data.readString();
                IBinder callback = data.readStrongBinder();

                Log.i(TAG, "exec: tool=" + toolName + " · args=" + argsJson
                        + " · caller uid=" + Binder.getCallingUid());

                if (callback == null) {
                    Log.w(TAG, "exec: sin callback, no hay a quien contestarle");
                    return true;
                }
                servir(toolName, callback);
                return true;
            }

            return super.onTransact(code, data, reply, flags);
        }
    };

    @Override
    public IBinder onBind(Intent intent) {
        Log.i(TAG, "service: bind · uid=" + Binder.getCallingUid());
        return binder;
    }

    /** Parte "<modulo>_<tool>" y se lo pasa a Gnothi por el IpcService que ya medimos. */
    private void servir(final String publicName, final IBinder callback) {

        final long t0 = System.currentTimeMillis();

        int cut = publicName == null ? -1 : publicName.indexOf('_');
        if (cut < 0) {
            contestar(callback, publicName, "nombre de tool invalido: " + publicName, false);
            return;
        }
        final String module = publicName.substring(0, cut);
        final String tool   = publicName.substring(cut + 1);

        final Context app = getApplicationContext();
        final ServiceConnection[] cell = new ServiceConnection[1];

        cell[0] = new ServiceConnection() {
            @Override public void onServiceConnected(ComponentName name, IBinder gnothiBinder) {
                Log.i(TAG, "exec: ligado a Gnothi a los " + (System.currentTimeMillis() - t0) + " ms");
                try {
                    Messenger gnothi = new Messenger(gnothiBinder);

                    Bundle datos = new Bundle();
                    datos.putString("module", module);
                    datos.putString("tool", tool);

                    Message msg = Message.obtain();
                    msg.setData(datos);
                    msg.replyTo = new Messenger(new Handler(Looper.getMainLooper()) {
                        @Override public void handleMessage(Message respuesta) {
                            Bundle d = respuesta.getData();
                            boolean autorizada = d != null && d.getBoolean("authorized", false);
                            String json = d == null ? "" : d.getString("json", "");

                            Log.i(TAG, "exec: Gnothi contesto a los "
                                    + (System.currentTimeMillis() - t0) + " ms · "
                                    + "authorized=" + autorizada + " · " + json);

                            contestar(callback, publicName, json, autorizada);
                            try { app.unbindService(cell[0]); } catch (Exception ignored) { }
                        }
                    });

                    gnothi.send(msg);
                } catch (RemoteException e) {
                    Log.e(TAG, "exec: fallo el envio a Gnothi", e);
                    contestar(callback, publicName, "fallo el envio a Gnothi: " + e.getMessage(), false);
                }
            }

            @Override public void onServiceDisconnected(ComponentName name) {
                Log.i(TAG, "exec: Gnothi se desconecto");
            }
        };

        Intent intent = new Intent();
        intent.setComponent(new ComponentName(GNOTHI_PKG, GNOTHI_SVC));

        boolean ok = app.bindService(intent, cell[0], Context.BIND_AUTO_CREATE);
        Log.i(TAG, "exec: bindService a Gnothi = " + ok);
        if (!ok) {
            contestar(callback, publicName, "no se pudo ligar a Gnothi", false);
        }
    }

    /** La transaccion de vuelta, con la forma exacta que Claude lee en su onTransact. */
    private void contestar(IBinder callback, String id, String resultado, boolean exito) {
        Parcel d = Parcel.obtain();
        try {
            d.writeInterfaceToken(CB_DESC);
            d.writeString(id);
            d.writeString(resultado);
            d.writeInt(exito ? 1 : 0);
            callback.transact(1, d, null, IBinder.FLAG_ONEWAY);
            Log.i(TAG, "exec: callback enviado · exito=" + exito);
        } catch (RemoteException e) {
            Log.e(TAG, "exec: el callback fallo", e);
        } finally {
            d.recycle();
        }
    }
}
