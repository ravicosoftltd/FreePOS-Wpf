﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FreePOS.data.viewmodel;
using FreePOS.bll;
using FreePOS.data.dapper;

namespace FreePOS.Views.finance
{
    /// <summary>
    /// Interaction logic for Window_NewSale.xaml
    /// </summary>
    public partial class pos : Window
    {
        List<productsaleorpurchaseviewmodel> mappedproducts;
        List<productsaleorpurchaseviewmodel> cart = new List<productsaleorpurchaseviewmodel>();
        data.dapper.user customer = null;
        productrepo productrepo = new productrepo();
        productsaleorpurchaseviewmodel lastInsertedProductInCart = null;
        public pos()
        {
            InitializeComponent();
            initFormOperations();
        }
        void initFormOperations()
        {
            numberOfReceiptsToPrint_tb.Text = AppSetting.NumberOfReceiptToPrint.ToString();
            BarcodeMode_cb.IsChecked = AppSetting.BarcodeMode;
            if ((bool)BarcodeMode_cb.IsChecked)
            {
                tb_Search.Text = "";
                tb_Search.Visibility = Visibility.Hidden;
                tb_Search_Barcode.Text = "";
                lv_SearchFoodItem.Visibility = Visibility.Hidden;
                tb_Search_Barcode.Visibility = Visibility.Visible;
                tb_Search_Barcode.Focus();
            }
            else
            {
                tb_Search.Text = "";
                tb_Search.Visibility = Visibility.Visible;
                tb_Search.Focus();
                tb_Search_Barcode.Text = "";
                tb_Search_Barcode.Visibility = Visibility.Hidden;
            }
            var products = this.productrepo.get();
            mappedproducts = productutils.mapproducttoproductsalemodel(products);
            cart_dg.ItemsSource = cart;
        }
        private void paying_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int total = Convert.ToInt32(paying_textbox.Text) - Convert.ToInt32(total_label.Content);
                change_label.Content = total;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void paying_textbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                doneSale();
            }
        }
        #region cart operations
        void addItem_To_cart(productsaleorpurchaseviewmodel item)
        {
            lastInsertedProductInCart = item;
            foreach (productsaleorpurchaseviewmodel oldItem in cart)
            {
                if (item.id == oldItem.id)
                {
                    oldItem.quantity += 1;
                    oldItem.total = oldItem.quantity * oldItem.price;
                    refreshCartAndTotal();
                    return;
                }
            }
            item.quantity = 1;
            cart.Add(item);
            refreshCartAndTotal();
        }
        private void removeItemFromCart_btn_click(object sender, RoutedEventArgs e)
        {
            productsaleorpurchaseviewmodel obj = ((FrameworkElement)sender).DataContext as productsaleorpurchaseviewmodel;
            removeItemFromCart(obj);
        }
        void removeItemFromCart(productsaleorpurchaseviewmodel item)
        {
            cart_dg.SelectedItem = null;
            lastInsertedProductInCart = null;
            foreach (productsaleorpurchaseviewmodel oldItem in cart)
            {
                if (item.id == oldItem.id)
                {
                    cart.Remove(item);
                    break;
                }
                lastInsertedProductInCart = oldItem;
            }
            if (lastInsertedProductInCart == null)
            {
                lastInsertedProductInCart = cart.LastOrDefault();
            }
            refreshCartAndTotal();
        }
        void minusQuantityOfItem_To_cart(productsaleorpurchaseviewmodel item)
        {
            lastInsertedProductInCart = item;
            foreach (productsaleorpurchaseviewmodel oldItem in cart)
            {
                if (item.id == oldItem.id)
                {
                    if (oldItem.quantity != 1) // if item qualtity in 1, we donot need to change quantity
                    {
                        oldItem.quantity -= 1;
                        oldItem.total = oldItem.quantity * oldItem.price;
                        refreshCartAndTotal();
                    }
                    return;

                }
            }
        }
        private async void cart_dg_roweditending(object sender, DataGridRowEditEndingEventArgs e)
        {
            productsaleorpurchaseviewmodel item = e.Row.Item as productsaleorpurchaseviewmodel;
            if (item != null)
            {
                foreach (productsaleorpurchaseviewmodel oldItem in cart)
                {
                    if (item.id == oldItem.id)
                    {
                        oldItem.total = oldItem.quantity * oldItem.price;
                        lastInsertedProductInCart = oldItem;
                        break;
                    }
                }
                await Task.Delay(TimeSpan.FromMilliseconds(5));
                refreshCartAndTotal();
            }
        }
        private void cart_dg_GotFocus(object sender, RoutedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            lastInsertedProductInCart = dg.CurrentItem as productsaleorpurchaseviewmodel;
        }

        private void refreshCartAndTotal()
        {
            try
            {
                cart_dg.SelectedItem = lastInsertedProductInCart;
                cart_dg.Items.Refresh();
                double totalBill1 = 0;
                foreach (productsaleorpurchaseviewmodel item1 in cart)
                {
                    totalBill1 += item1.total;
                }
                total_label.Content = totalBill1;
            }
            catch (Exception ex)
            {
                otherutils.notify("Info", "Update may not be applied", 10000);
                Console.WriteLine(ex);
            }
        }
        #endregion
        private void tb_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tb_Search.Text != "")
            {
                string s = tb_Search.Text;
                List<productsaleorpurchaseviewmodel> productList = mappedproducts.Where(a => a.name.ToLower().Contains(s.ToLower())).Take(6).ToList();
                lv_SearchFoodItem.ItemsSource = null;
                lv_SearchFoodItem.ItemsSource = productList;
                lv_SearchFoodItem.Visibility = Visibility.Visible;
                lv_SearchFoodItem.SelectedIndex = 0;
                lv_SearchFoodItem.Visibility = Visibility.Visible;
            }
            else { lv_SearchFoodItem.Visibility = Visibility.Hidden; }
        }
        private void tb_Search_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (tb_Search.Text != null && tb_Search.Text != "")
            {
                if (e.Key == Key.Down)
                {
                    int index = lv_SearchFoodItem.SelectedIndex + 1;
                    if (index < lv_SearchFoodItem.Items.Count)
                    {
                        lv_SearchFoodItem.SelectedIndex = index;
                    }
                    return;
                }
                else if (e.Key == Key.Up)
                {
                    int index = lv_SearchFoodItem.SelectedIndex - 1;
                    if (index > -1)
                    {
                        lv_SearchFoodItem.SelectedIndex = index;
                    }
                    return;
                }
                else if (e.Key == Key.Enter)
                {
                    if (lv_SearchFoodItem.SelectedItem != null)
                    {
                        productsaleorpurchaseviewmodel item = (productsaleorpurchaseviewmodel)lv_SearchFoodItem.SelectedItem;
                        addItem_To_cart(item);
                        tb_Search.Text = "";
                        lv_SearchFoodItem.Visibility = Visibility.Hidden;
                    }
                }
            }
            else
            {
                if (e.Key == Key.Up)
                {
                    if (lastInsertedProductInCart != null)
                    {
                        addItem_To_cart(lastInsertedProductInCart);
                        tb_Search.Text = "";
                        lv_SearchFoodItem.Visibility = Visibility.Hidden;
                    }
                }
                else if (e.Key == Key.Down)
                {
                    if (lastInsertedProductInCart != null)
                    {
                        minusQuantityOfItem_To_cart(lastInsertedProductInCart);
                        tb_Search.Text = "";
                        lv_SearchFoodItem.Visibility = Visibility.Hidden;
                    }
                }
                else if (e.Key == Key.Delete)
                {
                    if (lv_SearchFoodItem.SelectedItem != null)
                    {
                        removeItemFromCart(lastInsertedProductInCart);
                        tb_Search.Text = "";
                        lv_SearchFoodItem.Visibility = Visibility.Hidden;
                    }
                }
                else if (e.Key == Key.End)
                {
                    doneSale();
                }
            }
        }
        private void tb_Search_Barcode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (tb_Search_Barcode.Text != null && tb_Search_Barcode.Text != "")
            {
                if (e.Key == Key.Enter)
                {
                    productsaleorpurchaseviewmodel product = mappedproducts.Where(a => a.barcode == tb_Search_Barcode.Text).FirstOrDefault();
                    if (product != null)
                    {
                        addItem_To_cart(product);
                    }
                    tb_Search_Barcode.Text = "";

                }
            }
            else
            {
                if (e.Key == Key.Up)
                {
                    if (lastInsertedProductInCart != null)
                    {
                        addItem_To_cart(lastInsertedProductInCart);
                        tb_Search.Text = "";
                        lv_SearchFoodItem.Visibility = Visibility.Hidden;
                    }
                }
                else if (e.Key == Key.Down)
                {
                    if (lastInsertedProductInCart != null)
                    {
                        minusQuantityOfItem_To_cart(lastInsertedProductInCart);
                        tb_Search.Text = "";
                        lv_SearchFoodItem.Visibility = Visibility.Hidden;
                    }
                }
                else if (e.Key == Key.Delete)
                {
                    if (lv_SearchFoodItem.SelectedItem != null)
                    {
                        removeItemFromCart(lastInsertedProductInCart);
                        tb_Search.Text = "";
                        lv_SearchFoodItem.Visibility = Visibility.Hidden;
                    }
                }
                else if (e.Key == Key.End)
                {
                    doneSale();
                }
            }
        }
        private void BarcodeMode_cb_Checked(object sender, RoutedEventArgs e)
        {
            tb_Search.Text = "";
            tb_Search.Visibility = Visibility.Hidden;
            tb_Search_Barcode.Text = "";
            lv_SearchFoodItem.Visibility = Visibility.Hidden;
            tb_Search_Barcode.Visibility = Visibility.Visible;
            tb_Search_Barcode.Focus();

        }
        private void BarcodeMode_cb_UnChecked(object sender, RoutedEventArgs e)
        {
            tb_Search.Text = "";
            tb_Search.Visibility = Visibility.Visible;
            tb_Search.Focus();
            tb_Search_Barcode.Text = "";
            tb_Search_Barcode.Visibility = Visibility.Hidden;
        }
        private void customer_button_click(object sender, RoutedEventArgs e)
        {
            var dialog = new DialogSelectUser("customer");
            if (dialog.ShowDialog() == true)
            {
                customer = dialog.seleteduser;
                return;
            }
        }
        private void btn_doSale(object sender, RoutedEventArgs e)
        {
            doneSale();
        }
        void doneSale()
        {
            try
            {
                if (cart.Count == 0)
                {
                    MessageBox.Show("Add products to cart", "Information");
                    return;
                }
                var totalbill = cart.Sum(a => a.total);
                double totalpayment = 0;
                if (paying_textbox.Text != "")
                {
                    totalpayment = Convert.ToDouble(paying_textbox.Text);
                }
                var printCustomerInfoOnReceipt = true;
                var totalnumberofReceipts = 1;
                try {
                    totalnumberofReceipts = Convert.ToInt32(numberOfReceiptsToPrint_tb.Text);
                } catch { }
                saleutils.possale(cart, totalpayment - totalbill, customer, totalnumberofReceipts, printCustomerInfoOnReceipt);

                MessageBox.Show("Sale done Ammount :: " + totalbill, "Success");
                Close();
                new pos().Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sale not saved \n" + ex.Message, "Info");
            }

        }

    }

}
