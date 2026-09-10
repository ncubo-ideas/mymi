package com.ncubo.lab.octopus;

import android.app.Activity;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.os.Binder;
import android.os.Bundle;
import android.os.IBinder;
import android.os.Parcel;
import android.os.RemoteException;
import android.util.Log;
import android.widget.ScrollView;
import android.widget.TextView;

/**
 * EL AUTOTEST — hace EXACTAMENTE lo que hace Claude, para que si algo falla sepamos de que
 * lado esta. La caratula de AppFunctions nos enseño que sin esto uno depura a ciegas.
 *
 * Bindea a nuestro propio ToolExecService con la accion EXECUTE_TOOL, manda la transaccion 1
 * con (toolName, argsJson, callback) y espera la respuesta por el callback.
 */
public final class SelfTestActivity extends Activity {

    private static final String TAG = "OCTOPUS";
    private static final String SVC_DESC = "com.anthropic.claude.tool.thirdparty.IToolExecutionService";
    private static final String CB_DESC  = "com.anthropic.claude.tool.thirdparty.IToolExecutionCallback";

    private static final String[] PRUEBAS = {
        "facturas_total_de_facturas",
        "compras_total_de_compras",
        "examenes_revisar_mis_analitos"
    };

    private TextView out;
    private long t0;

    @Override protected void onCreate(Bundle saved) {
        super.onCreate(saved);
        out = new TextView(this);
        out.setPadding(36, 110, 36, 36);
        out.setTextSize(12f);
        ScrollView sv = new ScrollView(this);
        sv.addView(out);
        setContentView(sv);

        di("autotest del puente octopus");
        di("hace la MISMA transaccion que Claude");

        t0 = System.currentTimeMillis();

        Intent i = new Intent("com.anthropic.claude.tool.EXECUTE_TOOL");
        i.setPackage(getPackageName());

        boolean ok = bindService(i, new ServiceConnection() {
            @Override public void onServiceConnected(ComponentName n, IBinder b) {
                di("ligado al service a los " + (System.currentTimeMillis() - t0) + " ms");
                for (String tool : PRUEBAS) pedir(b, tool);
            }
            @Override public void onServiceDisconnected(ComponentName n) { }
        }, Context.BIND_AUTO_CREATE);

        di("bindService = " + ok);
    }

    /** La transaccion de ida, byte por byte como la escribe Claude. */
    private void pedir(IBinder servicio, final String tool) {
        final long t = System.currentTimeMillis();
        Parcel d = Parcel.obtain();
        try {
            d.writeInterfaceToken(SVC_DESC);
            d.writeString(tool);
            d.writeString("{}");
            d.writeStrongBinder(new Binder() {
                @Override protected boolean onTransact(int code, Parcel data, Parcel reply, int flags)
                        throws RemoteException {
                    if (code == INTERFACE_TRANSACTION) { reply.writeString(CB_DESC); return true; }
                    if (code == 1) {
                        data.enforceInterface(CB_DESC);
                        String id     = data.readString();
                        String result = data.readString();
                        boolean exito = data.readInt() != 0;
                        final String linea = (exito ? "OK  " : "FALLO  ") + id
                                + "  (" + (System.currentTimeMillis() - t) + " ms)\n      " + result;
                        runOnUiThread(() -> di(linea));
                        return true;
                    }
                    return super.onTransact(code, data, reply, flags);
                }
            });
            servicio.transact(1, d, null, IBinder.FLAG_ONEWAY);
            di("pedido " + tool);
        } catch (RemoteException e) {
            di("fallo el pedido de " + tool + ": " + e.getMessage());
        } finally {
            d.recycle();
        }
    }

    private void di(String s) {
        Log.i(TAG, "autotest: " + s);
        out.append(s + "\n\n");
    }
}
