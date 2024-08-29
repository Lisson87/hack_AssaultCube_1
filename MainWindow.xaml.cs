using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Swed32;

namespace hack_AssaultCube_1
{
    public partial class MainWindow : Window
    {
        bool gameIsRunning = false;
        //Invoke выполняется синхронно, а BeginInvoke - асинхронно.
        Swed swed;
        IntPtr moduleBase;

        List<IntPtr> listPointers = new List<IntPtr>();
        List<TextBox> checkedTboxes = new List<TextBox>();
        List<IntPtr> checkedPointers = new List<IntPtr>();
        
        IntPtr gun11Address;
        IntPtr gun12Address;
        IntPtr gun13Address;
        IntPtr gun14Address;
        IntPtr gun15Address;
        IntPtr pistolAddress;
        IntPtr grenadeAddress;
        

        Thread Thread_1Read;
        Thread Thread_2Freeze;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                swed = new Swed("ac_client"); //init swed library
                gameIsRunning = true;
            }
            catch (System.IndexOutOfRangeException)
            {
                this.Title = "Error. Game not running";
            }

            if (gameIsRunning)
            {
                moduleBase = swed.GetModuleBase("ac_client.exe");

                listPointers.Add(gun11Address);
                listPointers.Add(gun12Address);
                listPointers.Add(gun13Address);
                listPointers.Add(gun14Address);
                listPointers.Add(gun15Address);
                listPointers.Add(pistolAddress);
                listPointers.Add(grenadeAddress);

                gun11Address = swed.ReadPointer(moduleBase, 0x0017E0A8) + 0x140;
                gun12Address = swed.ReadPointer(moduleBase, 0x0017E0A8) + 0x138;
                gun13Address = swed.ReadPointer(moduleBase, 0x0017E0A8) + 0x13C;
                gun14Address = swed.ReadPointer(moduleBase, 0x0017E0A8) + 0x134;
                gun15Address = swed.ReadPointer(moduleBase, 0x0017E0A8) + 0x130;
                pistolAddress = swed.ReadPointer(moduleBase, 0x0017E0A8) + 0x12C;
                grenadeAddress = swed.ReadPointer(moduleBase, 0x0017E0A8) + 0x144;

                Thread_1Read = new Thread(Thread_ReadValues);
                Thread_1Read.Name = "thread_read_update";
                Thread_1Read.SetApartmentState(ApartmentState.STA);
                Thread_1Read.IsBackground = true;
                Thread_1Read.Start();
            }
        }

        public void Thread_ReadValues()
        {
            while (true)
            {
                Dispatcher.BeginInvoke(updateUI, Rifle11AmmoCurrent, swed.ReadInt(gun11Address));
                Dispatcher.BeginInvoke(updateUI, Rifle12AmmoCurrent, swed.ReadInt(gun12Address));
                Dispatcher.BeginInvoke(updateUI, Rifle13AmmoCurrent, swed.ReadInt(gun13Address));
                Dispatcher.BeginInvoke(updateUI, Rifle14AmmoCurrent, swed.ReadInt(gun14Address));
                Dispatcher.BeginInvoke(updateUI, Rifle15AmmoCurrent, swed.ReadInt(gun15Address));
                Dispatcher.BeginInvoke(updateUI, PistolAmmoCurrent, swed.ReadInt(pistolAddress));
                Dispatcher.BeginInvoke(updateUI, GrenadeAmmoCurrent, swed.ReadInt(grenadeAddress));
                Thread.Sleep(200);
            }
        }

        public void updateUI(object destination, object value)
        {
            int val = (int)value;
            TextBlock dest = (TextBlock)destination;
            dest.Text = val.ToString();
        }

        public int ReadValue(IntPtr valueAddress)
        {
            return swed.ReadInt(valueAddress);
        }

        // destination, value
        public void WriteValue(IntPtr valueAddress, int value)
        {
            swed.WriteInt(valueAddress, value);
        }

        private void Cbx_Checked()
        {
            if(Thread_2Freeze == null)
            {
                Thread_2Freeze = new Thread(T2);
                Thread_2Freeze.Name = "Thread_freeze_list";
                Thread_2Freeze.SetApartmentState(ApartmentState.STA);
                Thread_2Freeze.IsBackground = true;
                Thread_2Freeze.Start();
            }
            else if(Thread_2Freeze.ThreadState == ThreadState.Unstarted)
            {
                Thread_2Freeze.Start();
            }

        }

        public void T2()
        {
            List<int> list = new List<int>();
            int count0 = checkedTboxes.Count;
            // список полей для считывания при условии нажатого чекбокса
            //foreach (var textBox in checkedTboxes)
            //{
            //    object obj = Dispatcher.Invoke(ReadUI, textBox);
            //    Thread.Sleep(30);
            //    if (obj != null)
            //    {
            //        list.Add( (int)obj);
            //    }
            //    else
            //    {
            //        list.Add(99);
            //    }
            //}

            while (true)
            {
                if (checkedTboxes.Count != count0)
                {
                    list = new List<int>();
                    foreach (var textBox in checkedTboxes) {
                        object obj = Dispatcher.Invoke(ReadUI, textBox);
                        Thread.Sleep(30);
                        if (obj != null) {
                            list.Add((int)obj);
                        }
                        else {
                            list.Add(99);
                        }
                    }
                    count0 = checkedTboxes.Count;
                }

                // записываем по всем адресам в списке
                int i = 0;
                foreach (var val in list)
                {
                    swed.WriteInt(checkedPointers[i], val);
                    i++;
                }
                Thread.Sleep(150);
            }
        }

        public int ReadUI(object obj)
        {
            TextBox tbox = (TextBox)obj;
            int ammo = 55;
            int.TryParse(tbox.Text, out ammo);
            return ammo;
        }

        private void Cbx_Unchecked()
        {
            if(checkedTboxes == null)
            {
                Thread_2Freeze = new Thread(T2);
                Thread_2Freeze.Name = "Thread_freeze_list new";
                Thread_2Freeze.SetApartmentState(ApartmentState.STA);
                Thread_2Freeze.IsBackground = true;

            }
        }

        private void Rifle11AmmoCbx_Checked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Add(Rifle11AmmoTbx);
            checkedPointers.Add(gun11Address);
            Cbx_Checked();
        }

        private void Rifle11AmmoCbx_Unchecked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Remove(Rifle11AmmoTbx);
            checkedPointers.Remove(gun11Address);
            Cbx_Unchecked();
        }

        private void Rifle12AmmoCbx_Checked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Add(Rifle12AmmoTbx);
            checkedPointers.Add(gun12Address);
            Cbx_Checked();
        }

        private void Rifle12AmmoCbx_Unchecked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Remove(Rifle12AmmoTbx);
            checkedPointers.Remove(gun12Address);
            Cbx_Unchecked();
        }

        private void Rifle13AmmoCbx_Checked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Add(Rifle13AmmoTbx);
            checkedPointers.Add(gun13Address);
            Cbx_Checked();
        }

        private void Rifle13AmmoCbx_Unchecked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Remove(Rifle13AmmoTbx);
            checkedPointers.Remove(gun13Address);
            Cbx_Unchecked();
        }

        private void Rifle14AmmoCbx_Checked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Add(Rifle14AmmoTbx);
            checkedPointers.Add(gun14Address);
            Cbx_Checked();
        }

        private void Rifle14AmmoCbx_Unchecked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Remove(Rifle14AmmoTbx);
            checkedPointers.Remove(gun14Address);
            Cbx_Unchecked();
        }

        private void Rifle15AmmoCbx_Checked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Add(Rifle15AmmoTbx);
            checkedPointers.Add(gun15Address);
            Cbx_Checked();
        }

        private void Rifle15AmmoCbx_Unchecked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Remove(Rifle15AmmoTbx);
            checkedPointers.Remove(gun15Address);
            Cbx_Unchecked();
        }

        private void PistolAmmoCbx_Checked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Add(PistolAmmoTbx);
            checkedPointers.Add(pistolAddress);
            Cbx_Checked();
        }

        private void PistolAmmoCbx_Unchecked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Remove(PistolAmmoTbx);
            checkedPointers.Remove(pistolAddress);
            Cbx_Unchecked();
        }

        private void GrenadeAmmoCbx_Checked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Add(GrenadeAmmoTbx);
            checkedPointers.Add(grenadeAddress);
            Cbx_Checked();
        }

        private void GrenadeAmmoCbx_Unchecked(object sender, RoutedEventArgs e)
        {
            checkedTboxes.Remove(GrenadeAmmoTbx);
            checkedPointers.Remove(grenadeAddress);
            Cbx_Unchecked();
        }
    }
}